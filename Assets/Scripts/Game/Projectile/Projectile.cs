using Services.PoolService;
using Services.UpdateService;
using UnityEngine;
using VContainer;

public class Projectile : MonoBehaviour, IUpdatable, IPoolable
{
    [SerializeField] private float _speed = 15f;
    [SerializeField] private float _lifetime = 3f;

    private Vector3 _direction;
    private int _damage;
    private float _elapsedTime;
    private bool _isActive;

    private IUpdateSubscriptionService _updateService;
    private IGameObjectPoolService _poolService;

    [Inject]
    public void Construct(
        IUpdateSubscriptionService updateService,
        IGameObjectPoolService poolService)
    {
        _updateService = updateService;
        _poolService = poolService;
    }

    public void Initialize(Vector3 targetPosition, int damage)
    {
        _damage = damage;
        _direction = (targetPosition - transform.position).normalized;
        _elapsedTime = 0f;
        _isActive = true;

        if (_direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(_direction);
        }

        _updateService.RegisterUpdatable(this);
    }

    #region IUpdatable

    public void ManagedUpdate()
    {
        if (!_isActive)
            return;

        _elapsedTime += Time.deltaTime;

        if (_elapsedTime >= _lifetime)
        {
            ReturnToPool();
            return;
        }

        transform.position += _direction * _speed * Time.deltaTime;
    }

    #endregion

    #region IPoolable

    public void OnGetFromPool()
    {
        _elapsedTime = 0f;
        _isActive = false;
    }

    public void OnReturnToPool()
    {
        _isActive = false;
        _updateService?.UnregisterUpdatable(this);
        _direction = Vector3.zero;
        _damage = 0;
        _elapsedTime = 0f;
    }

    #endregion

    private void OnTriggerEnter(Collider other)
    {
        if (!_isActive)
            return;

        var slime = other.GetComponentInParent<BaseSlime>();
        if (slime == null)
            return;

        slime.TakeDamage(_damage);
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (!_isActive)
            return;

        _isActive = false;
        _updateService?.UnregisterUpdatable(this);
        _poolService.Release(gameObject);
    }
}
