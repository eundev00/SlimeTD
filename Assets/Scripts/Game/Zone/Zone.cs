using MessagePipe;
using UniRx;
using UnityEngine;
using VContainer;

public class Zone : MonoBehaviour
{
    [NotNull][SerializeField] private GridMapReference _gridMapReference;
    [NotNull][SerializeField] private WaveSpawner _waveSpawner;
    [NotNull][SerializeField] private TowerSpawner _towerSpawner;
    [NotNull][SerializeField] private TowerInputHandler _towerInputHandler;
    [NotNull][SerializeField] private Transform _spawnRoot;

    private IObjectResolver _resolver;
    private IGroundHeightSampler _groundHeightSampler;
    private IGameplayService _gameplayService;
    private ISubscriber<TowerSpawnRequestedEvent> _spawnRequestedSubscriber;

    private TowerCells _towerCells;
    private TowerMergeFinder _mergeFinder;
    private CompositeDisposable _disposables;

    public TowerInputHandler InputHandler => _towerInputHandler;

    [Inject]
    public void Construct(
        IObjectResolver resolver,
        IGroundHeightSampler groundHeightSampler,
        IGameplayService gameplayService,
        ISubscriber<TowerSpawnRequestedEvent> spawnRequestedSubscriber)
    {
        _resolver = resolver;
        _groundHeightSampler = groundHeightSampler;
        _gameplayService = gameplayService;
        _spawnRequestedSubscriber = spawnRequestedSubscriber;
    }

    public void Initialize(WaveTableData waveTable)
    {
        if (_gridMapReference == null || _gridMapReference.GridMapData == null)
        {
            Debug.Log("[Zone] GridMapReference가 연결되지 않았습니다.", this);
            return;
        }

        _towerCells = new TowerCells(_gridMapReference.GridMapData);
        _mergeFinder = new TowerMergeFinder(_towerCells, _gameplayService?.TierTable);

        if (_towerSpawner != null)
        {
            _resolver.Inject(_towerSpawner);
            _towerSpawner.Initialize(_towerCells, _spawnRoot);
        }

        if (_towerInputHandler != null)
        {
            _towerInputHandler.Initialize(_towerCells, _groundHeightSampler);
        }

        if (_waveSpawner != null)
        {
            _resolver.Inject(_waveSpawner);
            _waveSpawner.Initialize(waveTable, _spawnRoot);
        }

        _disposables?.Dispose();
        _disposables = new CompositeDisposable();
        _spawnRequestedSubscriber.Subscribe(e => OnTowerSpawnRequested(e.TowerData)).AddTo(_disposables);
    }

    // MessagePipe 브로커는 ProjectLifetimeScope 소속이라 씬을 넘어 산다. 여기서 안 끊으면 죽은 Zone이 계속 받는다.
    private void OnDestroy()
    {
        _disposables?.Dispose();
        _disposables = null;
    }

    private void OnTowerSpawnRequested(TowerData towerData)
    {
        if (_towerSpawner == null)
            return;

        _towerSpawner.TrySpawn(towerData);
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

        if (!_mergeFinder.TryFindMergePartner(source, out var sourceCell, out var partnerCell, out var partner))
            return false;

        if (!_mergeFinder.TryResolveMergeResult(sourceTower.Data, out var resultData))
            return false;

        return _towerSpawner.TryMerge(resultData, sourceCell, source, partnerCell, partner);
    }
}
