using MessagePipe;
using Services.PoolService;
using Services.UpdateService;
using UniRx;
using UnityEngine;
using VContainer;

public class BaseTower : MonoBehaviour, IPeriodicUpdatable
{
    [SerializeField] private float _attackRange = 5f;
    [SerializeField] private float _attackCooldown = 1f;
    [SerializeField] private int _damage = 1;
    [SerializeField] private Transform _firePoint;
    [NotNull][SerializeField] private GameObject _projectilePrefab;

    private TowerStats _stats;
    private IUpdateSubscriptionService _updateService;
    private IGameObjectPoolService _poolService;
    private ISubscriber<GameProgressEvent> _gameProgressSubscriber;
    private LayerMask _slimeLayer;

    private CompositeDisposable _disposables;
    private bool _stopped;

    private readonly Collider[] _hitBuffer = new Collider[32];

    public TowerStats Stats => _stats;

    [Inject]
    public void Construct(
        IUpdateSubscriptionService updateService,
        IGameObjectPoolService poolService,
        ISubscriber<GameProgressEvent> gameProgressSubscriber)
    {
        _updateService = updateService;
        _poolService = poolService;
        _gameProgressSubscriber = gameProgressSubscriber;
    }

    #region Unity Lifecycle

    private void Awake()
    {
        _slimeLayer = LayerMask.GetMask(GameTags.SlimeLayer);
        _stats = new TowerStats(_attackRange, _attackCooldown, _damage);
    }

    private void Start()
    {
        if (_firePoint == null)
        {
            Debug.LogWarning("[BaseTower] _firePoint가 연결되지 않았습니다. 타워 위치에서 발사합니다.", this);
        }

        if (_projectilePrefab == null)
        {
            Debug.LogError("[BaseTower] _projectilePrefab이 연결되지 않았습니다.", this);
            return;
        }

        _poolService.CreatePool(_projectilePrefab, 10, 50);
        _updateService.RegisterPeriodicUpdatable(this, _stats.AttackCooldown);

        _disposables = new CompositeDisposable();
        _gameProgressSubscriber.Subscribe(evt =>
        {
            if (evt.EventType == GameProgressType.GameOver)
                StopAttacking();
        }).AddTo(_disposables);
    }

    private void OnDestroy()
    {
        StopAttacking();
        _disposables?.Dispose();
        _disposables = null;
    }

    private void StopAttacking()
    {
        if (_stopped)
            return;

        _stopped = true;
        _updateService?.UnregisterPeriodicUpdatable(this);
    }

    #endregion

    #region IPeriodicUpdatable

    public void ManagedPeriodicUpdate(float deltaTime)
    {
        var target = FindClosestSlime();
        if (target == null)
            return;

        FireProjectile(target);
    }

    #endregion

    private Transform FindClosestSlime()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, _stats.AttackRange, _hitBuffer, _slimeLayer);

        if (count == 0)
            return null;

        Transform closest = null;
        float closestSqrDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            if (!_hitBuffer[i].gameObject.activeInHierarchy)
                continue;

            float sqrDist = (_hitBuffer[i].transform.position - transform.position).sqrMagnitude;
            if (sqrDist < closestSqrDist)
            {
                closestSqrDist = sqrDist;
                closest = _hitBuffer[i].transform;
            }
        }

        return closest;
    }

    private void FireProjectile(Transform target)
    {
        // 타워 회전 (타워 본체 기준, 수평 방향만)
        Vector3 horizontalDirection = (target.position - transform.position).normalized;
        horizontalDirection.y = 0;

        if (horizontalDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(horizontalDirection);
        }

        // 총알 생성
        var projObj = _poolService.Get(_projectilePrefab);
        if (projObj == null)
            return;

        // 발사 위치 (_firePoint가 있으면 그 위치, 없으면 타워 본체)
        Vector3 firePosition = _firePoint != null ? _firePoint.position : transform.position;
        projObj.transform.position = firePosition;

        var projectile = projObj.GetComponent<Projectile>();
        if (projectile == null)
        {
            Debug.LogError("[BaseTower] 발사체 프리팹에 Projectile 컴포넌트가 없습니다.", projObj);
            return;
        }

        // Projectile.Initialize가 발사 위치에서 타겟까지의 실제 3D 방향을 계산
        projectile.Initialize(target.position, _stats.Damage);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_stats != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _stats.AttackRange);
        }
        else
        {
            // Awake 전에는 SerializeField 값 사용
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _attackRange);
        }
    }
#endif
}
