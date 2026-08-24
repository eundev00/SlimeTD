using UnityEngine;
using VContainer;

public class TowerSpawner : ITowerSpawner
{
    private readonly IObjectResolver _resolver;
    private readonly IGameplayService _gameplayService;
    private readonly ITowerGridService _gridService;
    private readonly TowerSpawnConfig _config;

    private Transform _holder;

    public TowerSpawner(
        IObjectResolver resolver,
        IGameplayService gameplayService,
        ITowerGridService gridService,
        TowerSpawnConfig config)
    {
        _resolver = resolver;
        _gameplayService = gameplayService;
        _gridService = gridService;
        _config = config;
    }

    public bool TrySpawnRandom()
    {
        if (_config == null || _config.TowerPrefab == null)
        {
            Debug.LogError("[TowerSpawner] TowerSpawnConfig 또는 타워 프리팹이 없습니다.");
            return false;
        }

        var gridMapData = _gridService.GridMapData;
        if (gridMapData == null)
        {
            Debug.LogError("[TowerSpawner] GridMapData가 없어 타워를 배치할 수 없습니다.");
            return false;
        }

        if (!_gridService.TryGetRandomFreeCell(out var cell))
        {
            Debug.Log("[TowerSpawner] 배치 가능한 빈 칸이 없습니다.");
            return false;
        }

        if (!_config.IgnoreGoldCost && !_gameplayService.TrySpendGold(_config.Cost))
        {
            Debug.Log($"[TowerSpawner] 골드가 부족합니다. 필요: {_config.Cost}, 보유: {_gameplayService.Info.Gold.Value}");
            return false;
        }

        var position = GridUtility.GridToWorld(cell.x, cell.y, gridMapData);
        var tower = CreateTower(position);

        var handler = tower.GetComponent<ITowerInteractionHandler>();
        if (handler == null)
        {
            Debug.LogError("[TowerSpawner] 타워 프리팹에 ITowerInteractionHandler 구현체가 없습니다.", tower);
            return false;
        }

        _gridService.Register(cell, handler);
        return true;
    }

    private GameObject CreateTower(Vector3 position)
    {
        // 비활성 부모 밑에 생성해야 Awake가 안 돈다. 프리팹 자체를 SetActive(false)하면 에셋이 오염된다.
        var holder = GetHolder();
        var instance = Object.Instantiate(_config.TowerPrefab, holder);

        foreach (var component in instance.GetComponentsInChildren<MonoBehaviour>(true))
        {
            _resolver.Inject(component);
        }

        instance.transform.SetParent(null, false);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.identity;
        return instance;
    }

    private Transform GetHolder()
    {
        if (_holder != null)
            return _holder;

        var holderObject = new GameObject("[TowerSpawnHolder]");
        holderObject.SetActive(false);
        Object.DontDestroyOnLoad(holderObject);

        _holder = holderObject.transform;
        return _holder;
    }
}
