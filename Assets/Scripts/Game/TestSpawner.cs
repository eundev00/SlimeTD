using Services.PoolService;
using Services.UpdateService;
using UnityEngine;
using UnityEngine.Splines;
using VContainer;

public class TestSpawner : MonoBehaviour, IPeriodicUpdatable
{
    [SerializeField, NotNull] private GameObject _slimePrefab;
    [SerializeField, NotNull] private SplineContainer _splineContainer;
    [SerializeField] private float _spawnInterval = 2f;
    [SerializeField] private int _maxSlimes = 20;

    private IUpdateSubscriptionService _updateService;
    private IGameObjectPoolService _poolService;
    private int _spawnedCount;

    [Inject]
    public void Construct(
        IUpdateSubscriptionService updateService,
        IGameObjectPoolService poolService)
    {
        _updateService = updateService;
        _poolService = poolService;
    }

    #region Unity Lifecycle

    private void Start()
    {
        if (_slimePrefab == null)
        {
            Debug.LogError("[TestSpawner] _slimePrefab이 연결되지 않았습니다.", this);
            return;
        }

        if (_splineContainer == null)
        {
            Debug.LogError("[TestSpawner] _splineContainer가 연결되지 않았습니다.", this);
            return;
        }

        _poolService.CreatePool(_slimePrefab, 10, 50);
        _updateService.RegisterPeriodicUpdatable(this, _spawnInterval);
    }

    private void OnDestroy()
    {
        _updateService?.UnregisterPeriodicUpdatable(this);
    }

    #endregion

    #region IPeriodicUpdatable

    public void ManagedPeriodicUpdate(float deltaTime)
    {
        if (_spawnedCount >= _maxSlimes)
            return;

        SpawnSlime();
    }

    #endregion

    private void SpawnSlime()
    {
        var obj = _poolService.Get(_slimePrefab);
        if (obj == null)
            return;

        obj.transform.position = _splineContainer.EvaluatePosition(0f);

        var slime = obj.GetComponent<BaseSlime>();
        if (slime == null)
        {
            Debug.LogError("[TestSpawner] 슬라임 프리팹에 BaseSlime 컴포넌트가 없습니다.", obj);
            return;
        }

        slime.Initialize(_splineContainer);
        _spawnedCount++;
    }
}
