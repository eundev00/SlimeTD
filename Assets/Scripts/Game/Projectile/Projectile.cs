using Services.PoolService;
using System.Collections.Generic;
using Services.UpdateService;
using UnityEngine;
using VContainer;

public class Projectile : MonoBehaviour, IUpdatable, IPoolItem
{
    [SerializeField] private float _speed = 15f;
    [SerializeField] private float _lifetime = 3f;
    [SerializeField] private float _hitRadius = 0.15f;

    private Vector3 _direction;
    protected DamageInfo Info;
    private float _elapsedTime;
    private bool _isActive;

    private readonly RaycastHit[] _hitBuffer = new RaycastHit[8];
    private readonly List<BaseSlime> _hitSlimes = new List<BaseSlime>(4);
    private int _remainingPierce;
    private float _splashRadius;
    private int _slimeLayer;

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

    private void Awake()
    {
        _slimeLayer = LayerMask.GetMask(GameTags.SlimeLayer);
    }

    public void Initialize(Vector3 direction, DamageInfo info, int pierceCount = 0, float splashRadius = 0f)
    {
        Info = info;
        _remainingPierce = pierceCount;
        _splashRadius = splashRadius;
        _hitSlimes.Clear();
        _direction = direction.normalized;
        _elapsedTime = 0f;
        _isActive = true;

        if (_direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(_direction);
        }

        _updateService.RegisterUpdatable(this);
    }


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

        float distance = _speed * Time.deltaTime;

        transform.position += _direction * distance;

        DetectHits(distance);
    }

    private void DetectHits(float distance)
    {
        Vector3 origin = transform.position - _direction * distance;

        int count = Physics.SphereCastNonAlloc(
            origin, _hitRadius, _direction, _hitBuffer, distance, _slimeLayer);

        for (int i = 0; i < count && _isActive; i++)
        {
            int nearest = -1;
            for (int j = 0; j < count; j++)
            {
                if (_hitBuffer[j].distance < 0f)
                    continue;

                if (nearest < 0 || _hitBuffer[j].distance < _hitBuffer[nearest].distance)
                    nearest = j;
            }

            if (nearest < 0)
                break;

            var hit = _hitBuffer[nearest];
            _hitBuffer[nearest].distance = -1f;

            // 관통하는 동안 같은 슬라임을 다음 프레임에 또 맞히지 않게 이미 맞힌 대상을 기억한다.
            var slime = hit.collider.GetComponentInParent<BaseSlime>();
            if (slime != null && !slime.Stats.IsDead && !_hitSlimes.Contains(slime))
            {
                _hitSlimes.Add(slime);
                OnHit(slime);
            }
        }
    }



    public virtual void OnGetFromPool()
    {
        _elapsedTime = 0f;
        _isActive = false;
    }

    public virtual void OnReturnToPool()
    {
        _isActive = false;
        _updateService.UnregisterUpdatable(this);
        _direction = Vector3.zero;
        Info = default;
        _elapsedTime = 0f;
        _remainingPierce = 0;
        _splashRadius = 0f;
        _hitSlimes.Clear();
    }


    protected virtual void OnHit(BaseSlime slime)
    {
        slime.TakeDamage(Info);

        if (_splashRadius > 0f)
        {
            SplashDamage.Apply(slime.transform.position, _splashRadius, Info, slime);
            _splashRadius = 0f;
        }

        if (_remainingPierce > 0)
            _remainingPierce--;
        else
            ReturnToPool();
    }

    protected void ReturnToPool()
    {
        if (!_isActive)
            return;

        _isActive = false;
        _updateService.UnregisterUpdatable(this);
        _poolService.Release(gameObject);
    }

    private void OnDestroy()
    {
        _updateService.UnregisterUpdatable(this);
    }
}
