using Cysharp.Threading.Tasks;
using MessagePipe;
using UniRx;
using UnityEngine;
using VContainer;

public class Zone : MonoBehaviour
{
    [NotNull][SerializeField] private TowerSpawner _towerSpawner;
    [NotNull][SerializeField] private TowerInputHandler _towerInputHandler;
    [NotNull][SerializeField] private Transform _spawnRoot;

    private IObjectResolver _resolver;
    private IGroundHeightSampler _groundHeightSampler;
    private IGameplayService _gameplayService;
    private IAccountService _accountService;
    private ISingleBattleService _singleBattleService;
    private ICoopBattleService _coopBattleService;
    private ITowerPrefabProvider _prefabProvider;
    private ISubscriber<TowerSpawnRequestedEvent> _spawnRequestedSubscriber;

    private TowerCells _towerCells;
    private TowerPlacer _placer;
    private TowerMergeFinder _mergeFinder;
    private CompositeDisposable _disposables;
    private ICoopBattleClock _serverClock;

    public TowerInputHandler InputHandler => _towerInputHandler;

    [Inject]
    public void Construct(
        IObjectResolver resolver,
        IGroundHeightSampler groundHeightSampler,
        IGameplayService gameplayService,
        IAccountService accountService,
        ISubscriber<TowerSpawnRequestedEvent> spawnRequestedSubscriber,
        ISingleBattleService singleBattleService,
        ICoopBattleService coopBattleService,
        ITowerPrefabProvider prefabProvider)
    {
        _singleBattleService = singleBattleService;
        _coopBattleService = coopBattleService;
        _prefabProvider = prefabProvider;
        _resolver = resolver;
        _groundHeightSampler = groundHeightSampler;
        _gameplayService = gameplayService;
        _accountService = accountService;
        _spawnRequestedSubscriber = spawnRequestedSubscriber;
    }

    // serverClock이 있으면(협동) 소환·합성을 서버에 요청만 하고, 놓고 지우는 건 서버 이벤트(PlaceFromServer, RemoveFromServer)로 한다
    public void Initialize(MapZone mapZone, ICoopBattleClock serverClock = null)
    {
        if (mapZone == null || mapZone.GridMapData == null)
        {
            Debug.Log("[Zone] 맵 배치 구역의 그리드가 없습니다.", this);
            return;
        }

        _serverClock = serverClock;
        _towerCells = new TowerCells(mapZone.GridMapData);
        _mergeFinder = new TowerMergeFinder(_towerCells, _gameplayService?.Catalog, data => _accountService == null || _accountService.IsAvailable(data.Id));

        _placer?.Dispose();
        _placer = CreatePlacer(_towerCells);

        if (_towerSpawner != null)
        {
            _resolver.Inject(_towerSpawner);
            _towerSpawner.Initialize(_placer);
        }

        if (_towerInputHandler != null)
        {
            _towerInputHandler.Initialize(_towerCells);
            _towerInputHandler.EmptyCellTapped -= OnEmptyCellTapped;
            _towerInputHandler.EmptyCellTapped += OnEmptyCellTapped;
        }

        _disposables?.Dispose();
        _disposables = new CompositeDisposable();
        if (serverClock == null)
            _spawnRequestedSubscriber.Subscribe(e => OnTowerSpawnRequested(e.TowerData, e.Level)).AddTo(_disposables);
    }

    // 팀원 구역처럼 입력 없이 그리기만 하는 칸들. 부르는 쪽이 Dispose한다
    public TowerPlacer CreateViewOnlyPlacer(MapZone mapZone)
    {
        if (mapZone == null || mapZone.GridMapData == null)
            return null;

        return CreatePlacer(new TowerCells(mapZone.GridMapData));
    }

    private TowerPlacer CreatePlacer(TowerCells cells)
    {
        return new TowerPlacer(_resolver, _groundHeightSampler, _prefabProvider, cells, _spawnRoot, _serverClock);
    }

    public bool PlaceFromServer(TowerData towerData, Vector2Int cell, int level, out BaseTower tower)
    {
        tower = null;
        return _placer != null && _placer.TryPlace(towerData, cell, level, out tower);
    }

    public void RemoveFromServer(Vector2Int cell)
    {
        _placer?.Remove(cell);
    }

    // MessagePipe 브로커는 ProjectLifetimeScope 소속이라 씬을 넘어 산다. 여기서 안 끊으면 죽은 Zone이 계속 받는다.
    private void OnDestroy()
    {
        _disposables?.Dispose();
        _disposables = null;

        _placer?.Dispose();
        _placer = null;

        if (_towerInputHandler != null)
            _towerInputHandler.EmptyCellTapped -= OnEmptyCellTapped;
    }

    private void OnTowerSpawnRequested(TowerData towerData, int level)
    {
        if (_towerSpawner == null)
            return;

        _towerSpawner.TrySpawn(towerData, level: level);
    }

    private void OnEmptyCellTapped(Vector2Int cell)
    {
        if (_serverClock != null)
        {
            _coopBattleService.SummonAsync(cell.x, cell.y).Forget();
            return;
        }

        if (_towerSpawner == null)
            return;

        _towerSpawner.TrySpawn(null, cell);
    }

    public bool TryGetCellCenter(Vector3 worldPosition, out Vector3 cellCenter)
    {
        cellCenter = worldPosition;

        var gridMapData = _towerCells?.GridMapData;
        if (gridMapData == null)
            return false;

        (int x, int y) = GridUtility.WorldToGrid(worldPosition, gridMapData);
        if (!gridMapData.IsValidCoordinate(x, y))
            return false;

        cellCenter = GridUtility.GridToWorld(x, y, gridMapData);

        if (_groundHeightSampler != null)
            cellCenter = _groundHeightSampler.SnapToGround(cellCenter);

        return true;
    }

    public bool CanMerge(ITowerInteractionHandler source)
    {
        if (_mergeFinder == null)
            return false;

        return _mergeFinder.TryFindMergePartner(source, out _, out _, out _);
    }

    public bool TryMerge(ITowerInteractionHandler source)
    {
        if (_mergeFinder == null || _towerSpawner == null)
            return false;

        if (source is not BaseTower sourceTower || sourceTower == null)
            return false;

        if (!_mergeFinder.TryFindMergePartner(source, out var sourceCell, out var partnerCell, out _))
            return false;

        // 짝과 결과는 서버가 다시 정한다
        if (_serverClock != null)
        {
            _coopBattleService.MergeAsync(sourceCell.x, sourceCell.y).Forget();
            return true;
        }

        // 서버 검증: 합성이 실제로 일어날 때만 시드 목록에서 뽑고, 실패하면 되돌린다
        var random = _singleBattleService.MergeRandom;
        ulong savedRandom = random != null ? random.Save() : 0;
        if (_mergeFinder.TryResolveMergeResult(sourceTower.Data, out var resultData, random)
            && _towerSpawner.TryMerge(resultData, sourceCell, partnerCell))
            return true;

        random?.Restore(savedRandom);
        return false;
    }
}
