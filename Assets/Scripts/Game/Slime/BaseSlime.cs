using MessagePipe;
using UniRx;
using UnityEngine;
using UnityEngine.Splines;
using VContainer;

public class BaseSlime : MonoBehaviour, IPoolable
{
    #region Fields

    [SerializeField] protected int _maxHealth = 3;
    [SerializeField] protected float _moveSpeed = 10f;

    private SplineAnimate _splineAnimate;
    private ReactiveProperty<int> _currentHealth;
    private CompositeDisposable _disposables;
    private bool _isDead;

    private IPublisher<SlimeKilledEvent> _killedPublisher;
    private GameObjectPoolService _poolService;

    #endregion

    #region Properties

    protected SplineAnimate SplineAnimate => _splineAnimate;
    protected GameObjectPoolService PoolService => _poolService;
    protected bool IsDead => _isDead;

    public IReadOnlyReactiveProperty<int> CurrentHealth => _currentHealth;

    #endregion

    #region DI

    [Inject]
    public void Construct(
        IPublisher<SlimeKilledEvent> killedPublisher,
        GameObjectPoolService poolService)
    {
        _killedPublisher = killedPublisher;
        _poolService = poolService;
    }

    #endregion

    #region Unity Lifecycle

    protected virtual void Awake()
    {
        _splineAnimate = GetComponent<SplineAnimate>();
        if (_splineAnimate == null)
        {
            Debug.LogError("[BaseSlime] SplineAnimate 컴포넌트가 없습니다.", this);
            return;
        }

        _currentHealth = new ReactiveProperty<int>(_maxHealth);

        _splineAnimate.AnimationMethod = SplineAnimate.Method.Speed;
        _splineAnimate.MaxSpeed = _moveSpeed;
        _splineAnimate.Loop = SplineAnimate.LoopMode.Once;
    }

    #endregion

    #region IPoolable

    public virtual void OnGetFromPool()
    {
        _isDead = false;
        _disposables = new CompositeDisposable();
        _currentHealth.Value = _maxHealth;

        _currentHealth
            .Where(hp => hp <= 0)
            .Take(1)
            .Subscribe(_ => OnKilled())
            .AddTo(_disposables);

        if (_splineAnimate != null)
        {
            _splineAnimate.Completed += OnReachedEnd;
        }
    }

    public virtual void OnReturnToPool()
    {
        _disposables?.Dispose();
        _disposables = null;

        if (_splineAnimate != null)
        {
            _splineAnimate.Completed -= OnReachedEnd;
            _splineAnimate.Pause();
            _splineAnimate.NormalizedTime = 0f;
        }
    }

    #endregion

    #region Public API

    public virtual void Initialize(SplineContainer splineContainer)
    {
        if (_splineAnimate == null)
            return;

        _splineAnimate.Container = splineContainer;
        _splineAnimate.MaxSpeed = _moveSpeed;
        _splineAnimate.NormalizedTime = 0f;
        _splineAnimate.Play();
    }

    public virtual void TakeDamage(int damage)
    {
        if (_isDead || _currentHealth.Value <= 0)
            return;

        _currentHealth.Value -= damage;
    }

    #endregion

    #region Protected

    protected virtual void OnKilled()
    {
        if (_isDead)
            return;

        _isDead = true;

        _killedPublisher.Publish(new SlimeKilledEvent(
            gameObject.GetInstanceID(), transform.position));

        _poolService.Release(gameObject);
    }

    protected virtual void OnReachedEnd()
    {
        if (_isDead)
            return;

        _isDead = true;
        // TODO: 라이프 차감 이벤트 발행 (2단계)
        _poolService.Release(gameObject);
    }

    #endregion
}
