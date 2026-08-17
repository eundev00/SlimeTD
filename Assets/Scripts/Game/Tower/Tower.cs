using Services.UpdateService;
using UnityEngine;
using VContainer;

public class Tower : MonoBehaviour, IPeriodicUpdatable
{
    #region Fields

    [SerializeField] private float _attackRange = 5f;
    [SerializeField] private float _attackCooldown = 1f;
    [SerializeField] private int _damage = 1;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private LayerMask _slimeLayer;

    private IUpdateSubscriptionService _updateService;
    private GameObjectPoolService _poolService;

    private readonly Collider[] _hitBuffer = new Collider[32];

    #endregion

    #region DI

    [Inject]
    public void Construct(
        IUpdateSubscriptionService updateService,
        GameObjectPoolService poolService)
    {
        _updateService = updateService;
        _poolService = poolService;
    }

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        if (_firePoint == null)
        {
            Debug.LogError("[Tower] _firePoint가 연결되지 않았습니다.", this);
            return;
        }

        if (_projectilePrefab == null)
        {
            Debug.LogError("[Tower] _projectilePrefab이 연결되지 않았습니다.", this);
            return;
        }

        _poolService.CreatePool(_projectilePrefab, 10, 50);
        _updateService.RegisterPeriodicUpdatable(this, _attackCooldown);
    }

    private void OnDestroy()
    {
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

    #region Private

    private Transform FindClosestSlime()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, _attackRange, _hitBuffer, _slimeLayer);

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
        var projObj = _poolService.Get(_projectilePrefab);
        if (projObj == null)
            return;

        projObj.transform.position = _firePoint.position;

        var projectile = projObj.GetComponent<Projectile>();
        if (projectile == null)
        {
            Debug.LogError("[Tower] 발사체 프리팹에 Projectile 컴포넌트가 없습니다.", projObj);
            return;
        }

        projectile.Initialize(target.position, _damage);
    }

    #endregion

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
    #endif
}
