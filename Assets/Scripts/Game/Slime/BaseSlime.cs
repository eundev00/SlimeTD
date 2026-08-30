using Cysharp.Threading.Tasks;
using MessagePipe;
using Services.PoolService;
using System;
using System.Threading;
using UniRx;
using UnityEngine;
using UnityEngine.Splines;
using VContainer;

public class BaseSlime : MonoBehaviour, IPoolItem
{
    [SerializeField] private float _dieAnimationDuration = 1f;

    private SplineAnimate _splineAnimate;
    private SlimeStats _stats;
    private SlimeData _data;
    private IPublisher<SlimeKilledEvent> _killedPublisher;
    private IPublisher<SlimeReachedEndEvent> _reachedEndPublisher;
    private IGameObjectPoolService _poolService;
    private ISubscriber<GameProgressEvent> _gameProgressSubscriber;
    private CancellationTokenSource _cancellationTokenSource;
    private CompositeDisposable _disposables;
    private bool _gameOver;

    public SlimeStats Stats => _stats;

    [Inject]
    public void Construct(
        IPublisher<SlimeKilledEvent> killedPublisher,
        IPublisher<SlimeReachedEndEvent> reachedEndPublisher,
        IGameObjectPoolService poolService,
        ISubscriber<GameProgressEvent> gameProgressSubscriber)
    {
        _killedPublisher = killedPublisher;
        _reachedEndPublisher = reachedEndPublisher;
        _poolService = poolService;
        _gameProgressSubscriber = gameProgressSubscriber;
    }


    protected virtual void Awake()
    {
        _splineAnimate = GetComponent<SplineAnimate>();
        if (_splineAnimate == null)
        {
            Debug.Log("[BaseSlime] SplineAnimate 컴포넌트가 없습니다.", this);
            return;
        }

        // SlimeAnimation/SlimeFace가 OnGetFromPool에서 이 인스턴스를 구독하므로 교체하면 구독이 끊긴다.
        _stats = new SlimeStats(0);

        _splineAnimate.AnimationMethod = SplineAnimate.Method.Speed;
        _splineAnimate.Loop = SplineAnimate.LoopMode.Once;
    }



    public virtual void OnGetFromPool()
    {
        _gameOver = false;
        _cancellationTokenSource = new CancellationTokenSource();

        _disposables = new CompositeDisposable();
        _gameProgressSubscriber?.Subscribe(evt =>
        {
            if (evt.EventType == GameProgressType.GameOver)
                StopMoving();
        }).AddTo(_disposables);

        if (_splineAnimate != null)
        {
            _splineAnimate.Completed += OnReachedEnd;
        }
    }

    public virtual void OnReturnToPool()
    {
        // 0으로 눕혀야 재사용 시 SlimeFace/SlimeAnimation의 Pairwise가 헛피격으로 오인하지 않는다.
        _stats?.Reset(0);

        _disposables?.Dispose();
        _disposables = null;

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

    private void StopMoving()
    {
        _gameOver = true;

        if (_splineAnimate != null)
            _splineAnimate.Pause();
    }



    public virtual void Initialize(SplineContainer splineContainer, SlimeData data, int health)
    {
        if (_splineAnimate == null || data == null)
            return;

        _data = data;
        _stats.Reset(health);

        _splineAnimate.Container = splineContainer;
        _splineAnimate.MaxSpeed = data.BaseSpeed;
        _splineAnimate.NormalizedTime = 0f;

        if (!_gameOver)
            _splineAnimate.Play();
    }

    public virtual void TakeDamage(int damage)
    {
        _stats.TakeDamage(damage);

        if (_stats.IsDead)
        {
            _splineAnimate.Pause();
            _killedPublisher.Publish(new SlimeKilledEvent(_data.GoldReward));
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
        _reachedEndPublisher.Publish(new SlimeReachedEndEvent(_data.LifeCost));
        _poolService.Release(gameObject);
    }

    protected virtual void OnDestroy()
    {
        _disposables?.Dispose();
        _disposables = null;

        _stats?.Dispose();
    }
}
