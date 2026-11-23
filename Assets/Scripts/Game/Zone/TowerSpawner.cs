using UnityEngine;
using VContainer;

public class TowerSpawner : MonoBehaviour
{
    private const int DefaultSummonGradeId = 1;

    private IObjectResolver _resolver;
    private IGameplayService _gameplayService;
    private IGroundHeightSampler _groundHeightSampler;
    private IAccountService _accountService;
    private ITowerPrefabProvider _prefabProvider;
    private ISingleBattleService _singleBattleService;
    private TowerCells _towerCells;

    private Transform _spawnRoot;
    private Transform _holder;

    [Inject]
    public void Construct(
        IObjectResolver resolver,
        IGameplayService gameplayService,
        IGroundHeightSampler groundHeightSampler,
        IAccountService accountService,
        ITowerPrefabProvider prefabProvider,
        ISingleBattleService singleBattleService)
    {
        _singleBattleService = singleBattleService;
        _resolver = resolver;
        _gameplayService = gameplayService;
        _groundHeightSampler = groundHeightSampler;
        _accountService = accountService;
        _prefabProvider = prefabProvider;
    }

    public void Initialize(TowerCells towerCells, Transform spawnRoot)
    {
        _towerCells = towerCells;
        _spawnRoot = spawnRoot;
    }

    private void OnDestroy()
    {
        if (_holder != null)
        {
            Destroy(_holder.gameObject);
        }
    }

    public bool TrySpawn(TowerData towerData = null, Vector2Int? targetCell = null)
    {
        if (towerData == null && !TryDrawTower(out towerData))
            return false;

        if (_resolver == null || _towerCells == null)
        {
            Debug.Log("[TowerSpawner] Initialize가 호출되지 않았습니다.", this);
            return false;
        }

        var gridMapData = _towerCells.GridMapData;
        if (gridMapData == null)
        {
            Debug.Log("[TowerSpawner] GridMapData가 없어 타워를 배치할 수 없습니다.", this);
            return false;
        }

        Vector2Int cell;
        if (targetCell.HasValue)
        {
            cell = targetCell.Value;
            if (!_towerCells.IsFree(cell))
                return false;
        }
        else if (!_towerCells.TryGetRandomFreeCell(out cell))
        {
            Debug.Log("[TowerSpawner] 배치 가능한 빈 칸이 없습니다.", this);
            return false;
        }

        if (!_prefabProvider.TryGet(towerData, out var prefab))
        {
            Debug.Log($"[TowerSpawner] 타워 프리팹이 없습니다: {towerData.CodeName}", this);
            return false;
        }

        // 골드 차감은 실패 가능한 검증을 모두 통과한 뒤에 한다. 차감 후 실패하면 환불 경로가 없다.
        if (!_gameplayService.TrySpendSummonCost())
        {
            Debug.Log($"[TowerSpawner] 골드가 부족합니다. 필요: {_gameplayService.Info.SummonCost.Value}, 보유: {_gameplayService.Info.Gold.Value}", this);
            return false;
        }

        var position = GridUtility.GridToWorld(cell.x, cell.y, gridMapData);
        position = _groundHeightSampler.SnapToGround(position);

        var tower = CreateTower(prefab, towerData, position);

        var handler = tower.GetComponent<ITowerInteractionHandler>();
        if (handler == null)
        {
            Debug.Log("[TowerSpawner] 타워 프리팹에 ITowerInteractionHandler 구현체가 없습니다.", tower);
            Destroy(tower);
            return false;
        }

        _towerCells.Register(cell, handler);
        SetCell(handler, cell);
        _singleBattleService.RecordSummon(cell, towerData.Id);
        return true;
    }

    public bool TryMerge(
        TowerData resultData,
        Vector2Int resultCell,
        ITowerInteractionHandler source,
        Vector2Int partnerCell,
        ITowerInteractionHandler partner)
    {
        if (resultData == null || !_prefabProvider.TryGet(resultData, out var resultPrefab))
        {
            Debug.Log("[TowerSpawner] 합성 결과 타워에 프리팹이 없습니다.", this);
            return false;
        }

        if (_resolver == null || _towerCells == null)
        {
            Debug.Log("[TowerSpawner] Initialize가 호출되지 않았습니다.", this);
            return false;
        }

        var gridMapData = _towerCells.GridMapData;
        if (gridMapData == null)
        {
            Debug.Log("[TowerSpawner] GridMapData가 없어 타워를 배치할 수 없습니다.", this);
            return false;
        }

        _towerCells.Unregister(resultCell);
        _towerCells.Unregister(partnerCell);

        if (source is MonoBehaviour sourceBehaviour && sourceBehaviour != null)
            Destroy(sourceBehaviour.gameObject);

        if (partner is MonoBehaviour partnerBehaviour && partnerBehaviour != null)
            Destroy(partnerBehaviour.gameObject);

        var position = GridUtility.GridToWorld(resultCell.x, resultCell.y, gridMapData);
        position = _groundHeightSampler.SnapToGround(position);

        var tower = CreateTower(resultPrefab, resultData, position);

        var handler = tower.GetComponent<ITowerInteractionHandler>();
        if (handler == null)
        {
            Debug.Log("[TowerSpawner] 합성 결과 프리팹에 ITowerInteractionHandler 구현체가 없습니다.", tower);
            Destroy(tower);
            return false;
        }

        _towerCells.Register(resultCell, handler);
        SetCell(handler, resultCell);
        _singleBattleService.RecordMerge(resultCell, partnerCell, resultData.Id);
        return true;
    }

    private static void SetCell(ITowerInteractionHandler handler, Vector2Int cell)
    {
        if (handler is BaseTower tower)
            tower.Cell = cell;
    }

    private bool TryDrawTower(out TowerData towerData)
    {
        towerData = null;

        var catalog = _gameplayService?.Catalog;
        if (catalog == null)
        {
            Debug.Log("[TowerSpawner] TowerCatalog가 없어 타워를 뽑을 수 없습니다.", this);
            return false;
        }

        var config = _gameplayService.Config;
        int gradeId = config != null ? config.SummonGradeId : DefaultSummonGradeId;

        if (!catalog.TryDraw(gradeId, out towerData, IsAvailable))
        {
            Debug.Log($"[TowerSpawner] 등급 {gradeId}에서 뽑을 타워가 없습니다.", this);
            return false;
        }

        return true;
    }

    private bool IsAvailable(TowerData towerData)
    {
        return _accountService == null || _accountService.IsAvailable(towerData.Id);
    }

    private GameObject CreateTower(GameObject prefab, TowerData towerData, Vector3 position)
    {
        return TowerFactory.Create(_resolver, prefab, towerData, GetHolder(), _spawnRoot, position);
    }

    private Transform GetHolder()
    {
        if (_holder != null)
            return _holder;

        var holderObject = new GameObject("[TowerSpawnHolder]");
        holderObject.SetActive(false);

        _holder = holderObject.transform;
        return _holder;
    }
}
