using Cysharp.Threading.Tasks;
using MessagePipe;
using Services.PoolService;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Splines;
using VContainer;

public class BaseSlime : MonoBehaviour, IPoolable
{
    [SerializeField] protected int _maxHealth = 3;
    [SerializeField] protected float _moveSpeed = 1f;
    [SerializeField] private float _dieAnimationDuration = 1f;

    private SplineAnimate _splineAnimate;
    private SlimeStats _stats;
    private IPublisher<SlimeKilledEvent> _killedPublisher;
    private IGameObjectPoolService _poolService;
    private CancellationTokenSource _cancellationTokenSource;

    public SlimeStats Stats => _stats;

    [Inject]
    public void Construct(
        IPublisher<SlimeKilledEvent> killedPublisher,
        IGameObjectPoolService poolService)
    {
        _killedPublisher = killedPublisher;
        _poolService = poolService;
    }

    #region Unity Lifecycle

    protected virtual void Awake()
    {
        _splineAnimate = GetComponent<SplineAnimate>();
        if (_splineAnimate == null)
        {
            Debug.LogError("[BaseSlime] SplineAnimate 컴포넌트가 없습니다.", this);
            return;
        }

        _stats = new SlimeStats(_maxHealth);

        _splineAnimate.AnimationMethod = SplineAnimate.Method.Speed;
        _splineAnimate.MaxSpeed = _moveSpeed;
        _splineAnimate.Loop = SplineAnimate.LoopMode.Once;
    }

    #endregion

    #region IPoolable

    public virtual void OnGetFromPool()
    {
        _stats.Reset(_maxHealth);
        _cancellationTokenSource = new CancellationTokenSource();

        if (_splineAnimate != null)
        {
            _splineAnimate.Completed += OnReachedEnd;
        }
    }

    public virtual void OnReturnToPool()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

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
        _stats.TakeDamage(damage);

        if (_stats.IsDead)
        {
            _splineAnimate.Pause();
            _killedPublisher.Publish(new SlimeKilledEvent());
            OnDiedAsync();
        }
    }

    #endregion


    private async void OnDiedAsync()
    {
        try
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(_dieAnimationDuration),
                cancellationToken: _cancellationTokenSource.Token);

            if (_poolService != null)
            {
                _poolService.Release(gameObject);
            }
        }
        catch (OperationCanceledException)
        {
            // 풀 반환 시 취소되면 무시
        }
    }

    protected virtual void OnReachedEnd()
    {
        if (_stats.IsDead == false)
            return;

        // TODO: 라이프 차감 이벤트 발행 (2단계)
        _poolService.Release(gameObject);
    }
}
