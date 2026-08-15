using System;
using MessagePipe;
using UniRx;
using UnityEngine;
using UnityEngine.Splines;
using VContainer;

/// 슬라임 MonoBehaviour. Spline 경로 이동, 체력 관리, 풀 반환을 담당한다.
/// 비주얼(Mesh/Material)과 로직을 분리하여 나중에 아트 리소스 교체가 가능하도록 한다.
public class SlimeView : MonoBehaviour, IPoolable
{
    #region Fields

    [SerializeField] private int _maxHealth = 3;
    [SerializeField] private float _moveSpeed = 3f;

    private SplineAnimate _splineAnimate;
    private ReactiveProperty<int> _currentHealth;
    private CompositeDisposable _disposables;
    private bool _isDead;

    private IPublisher<SlimeKilledEvent> _killedPublisher;
    private GameObjectPoolService _poolService;

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

    private void Awake()
    {
        _splineAnimate = GetComponent<SplineAnimate>();
        if (_splineAnimate == null)
        {
            Debug.LogError("[SlimeView] SplineAnimate 컴포넌트가 없습니다.", this);
            return;
        }

        _currentHealth = new ReactiveProperty<int>(_maxHealth);

        // SplineAnimate 기본 설정 (프리팹 Inspector에서도 설정 가능하지만 코드로 보장)
        _splineAnimate.AnimationMethod = SplineAnimate.Method.Speed;
        _splineAnimate.MaxSpeed = _moveSpeed;
        _splineAnimate.Loop = SplineAnimate.LoopMode.Once;
    }

    #endregion

    #region IPoolable

    public void OnGetFromPool()
    {
        _isDead = false;
        _disposables = new CompositeDisposable();
        _currentHealth.Value = _maxHealth;

        // 체력 0 이하 → 처치
        _currentHealth
            .Where(hp => hp <= 0)
            .Take(1)
            .Subscribe(_ => OnKilled())
            .AddTo(_disposables);

        // 경로 끝 도달 이벤트 구독
        if (_splineAnimate != null)
        {
            _splineAnimate.Completed += OnReachedEnd;
        }
    }

    public void OnReturnToPool()
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

    /// 스포너가 풀에서 꺼낸 뒤 호출. Spline 경로를 지정하고 이동을 시작한다.
    public void Initialize(SplineContainer splineContainer)
    {
        if (_splineAnimate == null)
            return;

        _splineAnimate.Container = splineContainer;
        _splineAnimate.MaxSpeed = _moveSpeed;
        _splineAnimate.NormalizedTime = 0f;
        _splineAnimate.Play();
    }

    public void TakeDamage(int damage)
    {
        if (_isDead || _currentHealth.Value <= 0)
            return;

        _currentHealth.Value -= damage;
    }

    /// 분열 시스템(3단계)에서 임계치 감지에 사용할 수 있도록 노출
    public IReadOnlyReactiveProperty<int> CurrentHealth => _currentHealth;

    #endregion

    #region Private

    private void OnKilled()
    {
        if (_isDead)
            return;

        _isDead = true;

        _killedPublisher.Publish(new SlimeKilledEvent(
            gameObject.GetInstanceID(), transform.position));

        _poolService.Release(gameObject);
    }

    /// 경로 끝 도달 시 호출. 라이프 차감은 2단계 작업에서 추가 예정.
    private void OnReachedEnd()
    {
        if (_isDead)
            return;

        _isDead = true;
        // TODO: 라이프 차감 이벤트 발행 (2단계)
        _poolService.Release(gameObject);
    }

    #endregion
}
