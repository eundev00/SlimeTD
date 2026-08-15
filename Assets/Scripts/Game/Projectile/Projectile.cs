using Services.UpdateService;
using UnityEngine;
using VContainer;

/// 논호밍 직선 발사체. 발사 시점의 타겟 위치를 향해 직진하며, 도중에 슬라임과 충돌하면 데미지를 준다.
public class Projectile : MonoBehaviour, IUpdatable, IPoolable
{
    #region Fields

    [SerializeField] private float _speed = 15f;
    [SerializeField] private float _lifetime = 3f;

    private Vector3 _direction;
    private int _damage;
    private float _elapsedTime;
    private bool _isActive;

    private IUpdateSubscriptionService _updateService;
    private GameObjectPoolService _poolService;

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

    #region Public API

    /// 타워가 발사 시 호출. 타겟의 현재 위치를 향해 방향을 고정하고 이동을 시작한다.
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

    #endregion

    #region IUpdatable

    public void ManagedUpdate()
    {
        if (!_isActive)
            return;

        _elapsedTime += Time.deltaTime;

        // 수명 초과 → 풀 반환
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

    #region Collision

    private void OnTriggerEnter(Collider other)
    {
        if (!_isActive)
            return;

        var slime = other.GetComponent<SlimeView>();
        if (slime == null)
            return;

        slime.TakeDamage(_damage);
        ReturnToPool();
    }

    #endregion

    #region Private

    private void ReturnToPool()
    {
        if (!_isActive)
            return;

        _isActive = false;
        _poolService.Release(gameObject);
    }

    #endregion
}
