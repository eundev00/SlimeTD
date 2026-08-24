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
    [SerializeField] private int _lifeCost = 1;
    [SerializeField] private int _goldReward = 10;

    private SplineAnimate _splineAnimate;
    private SlimeStats _stats;
    private IPublisher<SlimeKilledEvent> _killedPublisher;
    private IPublisher<SlimeReachedEndEvent> _reachedEndPublisher;
    private IGameObjectPoolService _poolService;
    private CancellationTokenSource _cancellationTokenSource;

    public SlimeStats Stats => _stats;

    [Inject]
    public void Construct(
        IPublisher<SlimeKilledEvent> killedPublisher,
        IPublisher<SlimeReachedEndEvent> reachedEndPublisher,
        IGameObjectPoolService poolService)
    {
        _killedPublisher = killedPublisher;
        _reachedEndPublisher = reachedEndPublisher;
        _poolService = poolService;
    }


    protected virtual void Awake()
    {
        _splineAnimate = GetComponent<SplineAnimate>();
        if (_splineAnimate == null)
        {
            Debug.Log("[BaseSlime] SplineAnimate 컴포넌트가 없습니다.", this);
            return;
        }

        _stats = new SlimeStats(_maxHealth);

        _splineAnimate.AnimationMethod = SplineAnimate.Method.Speed;
        _splineAnimate.MaxSpeed = _moveSpeed;
        _splineAnimate.Loop = SplineAnimate.LoopMode.Once;
    }



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
            _killedPublisher.Publish(new SlimeKilledEvent(_goldReward));
            OnDiedAsync();
        }
    }



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
        // 처치 시 SplineAnimate.Pause로 Completed가 막히므로 여기 도달은 "살아서 끝까지 온" 경우다.
        // 처치와 도달이 같은 슬라임에서 함께 발생하지 않아 WaveSpawner 카운터 이중 차감이 없다.
        _reachedEndPublisher.Publish(new SlimeReachedEndEvent(_lifeCost));
        _poolService.Release(gameObject);
    }
}
