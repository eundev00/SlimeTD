using UnityEngine;
using VContainer;

public class TowerSpawner : MonoBehaviour
{
    private const int DrawableTier = 1;

    private IObjectResolver _resolver;
    private IGameplayService _gameplayService;
    private IGroundHeightSampler _groundHeightSampler;
    private TowerCells _towerCells;

    private Transform _spawnRoot;
    private Transform _holder;

    [Inject]
    public void Construct(
        IObjectResolver resolver,
        IGameplayService gameplayService,
        IGroundHeightSampler groundHeightSampler)
    {
        _resolver = resolver;
        _gameplayService = gameplayService;
        _groundHeightSampler = groundHeightSampler;
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

    public bool TrySpawnRandom()
    {
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

        if (!_towerCells.TryGetRandomFreeCell(out var cell))
        {
            Debug.Log("[TowerSpawner] 배치 가능한 빈 칸이 없습니다.", this);
            return false;
        }

        var tierTable = _gameplayService.TierTable;
        if (tierTable == null)
        {
            Debug.Log("[TowerSpawner] TowerTierTable이 없어 타워를 뽑을 수 없습니다.", this);
            return false;
        }

        if (!tierTable.TryDraw(DrawableTier, out var towerData))
        {
            Debug.Log($"[TowerSpawner] 티어 {DrawableTier}에서 뽑을 타워가 없습니다.", this);
            return false;
        }

        if (towerData.Prefab == null)
        {
            Debug.Log("[TowerSpawner] 선택된 TowerData에 프리팹이 없습니다.", this);
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

        var tower = CreateTower(towerData, position);

        var handler = tower.GetComponent<ITowerInteractionHandler>();
        if (handler == null)
        {
            Debug.Log("[TowerSpawner] 타워 프리팹에 ITowerInteractionHandler 구현체가 없습니다.", tower);
            Destroy(tower);
            return false;
        }

        _towerCells.Register(cell, handler);
        return true;
    }

    private GameObject CreateTower(TowerData towerData, Vector3 position)
    {
        // 비활성 부모 밑에 생성해야 Awake가 안 돈다. 프리팹 자체를 SetActive(false)하면 에셋이 오염된다.
        var holder = GetHolder();
        var instance = Instantiate(towerData.Prefab, holder);

        foreach (var component in instance.GetComponentsInChildren<MonoBehaviour>(true))
        {
            _resolver.Inject(component);
        }

        instance.transform.SetParent(_spawnRoot, false);
        instance.transform.position = position;

        // 카메라를 바라보도록 회전 (Y축만 회전)
        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            var directionToCamera = mainCamera.transform.position - position;
            directionToCamera.y = 0; // 수평 방향만 계산
            if (directionToCamera.sqrMagnitude > 0.001f)
            {
                instance.transform.rotation = Quaternion.LookRotation(directionToCamera);
            }
            else
            {
                instance.transform.rotation = Quaternion.identity;
            }
        }
        else
        {
            instance.transform.rotation = Quaternion.identity;
        }

        var tower = instance.GetComponent<BaseTower>();
        if (tower != null)
        {
            tower.Initialize(towerData);
        }

        return instance;
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
