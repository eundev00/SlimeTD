using Cysharp.Threading.Tasks;
using MessagePipe;
using Services.PoolService;
using Services.UpdateService;
using System;
using System.Threading;
using UniRx;
using UnityEngine;
using UnityEngine.Splines;
using VContainer;

public class BaseSlime : MonoBehaviour, ISlime, IPoolItem, IUpdatable
{
    private static readonly int DepthBucketId = Shader.PropertyToID("_SlimeDepthBucket");

    [SerializeField] private float _dieAnimationDuration = 1f;
    [SerializeField] private SkinnedMeshRenderer[] _depthBucketRenderers;
    [NotNull][SerializeField] private Transform _damageTextAnchor;

    private SplineAnimate _splineAnimate;
    private MaterialPropertyBlock _propertyBlock;
    private SlimeStats _stats;
    private SlimeDataBase _data;
    private int _jellyReward;
    private IPublisher<SlimeKilledEvent> _killedPublisher;
    private IPublisher<SlimeReachedEndEvent> _reachedEndPublisher;
    private IGameObjectPoolService _poolService;
    private ISubscriber<GameProgressEvent> _gameProgressSubscriber;
    private DamageTextSpawner _damageTextSpawner;
    private CancellationTokenSource _cancellationTokenSource;
    private CompositeDisposable _disposables;
    private bool _gameOver;

    private IUpdateSubscriptionService _updateService;
    private IGameplayService _gameplayService;
    private readonly SlimeSlow _slow = new SlimeSlow();
    private bool _slowRegistered;

    public SlimeStats Stats => _stats;
    public Vector3 Position => transform.position;

    [Inject]
    public void Construct(
        IPublisher<SlimeKilledEvent> killedPublisher,
        IPublisher<SlimeReachedEndEvent> reachedEndPublisher,
        IGameObjectPoolService poolService,
        ISubscriber<GameProgressEvent> gameProgressSubscriber,
        DamageTextSpawner damageTextSpawner,
        IUpdateSubscriptionService updateService,
        IGameplayService gameplayService)
    {
        _killedPublisher = killedPublisher;
        _reachedEndPublisher = reachedEndPublisher;
        _poolService = poolService;
        _gameProgressSubscriber = gameProgressSubscriber;
        _damageTextSpawner = damageTextSpawner;
        _updateService = updateService;
        _gameplayService = gameplayService;
    }


    protected virtual void Awake()
    {
        _splineAnimate = GetComponent<SplineAnimate>();
        if (_splineAnimate == null)
        {
            Debug.Log("[BaseSlime] SplineAnimate 컴포넌트가 없습니다.", this);
            return;
        }

        if (_damageTextAnchor == null)
        {
            Debug.Log("[BaseSlime] _damageTextAnchor가 연결되지 않아 슬라임 위치에 데미지 텍스트를 띄웁니다.", this);
            _damageTextAnchor = transform;
        }

        if (_depthBucketRenderers == null || _depthBucketRenderers.Length == 0)
            Debug.Log("[BaseSlime] _depthBucketRenderers가 연결되지 않아 렌더 순서를 적용할 수 없습니다.", this);
        else
            _propertyBlock = new MaterialPropertyBlock();

        _stats = new SlimeStats(0);

        _splineAnimate.AnimationMethod = SplineAnimate.Method.Speed;
        _splineAnimate.Loop = SplineAnimate.LoopMode.Once;
    }



    public virtual void OnGetFromPool()
    {
        _gameOver = false;
        _cancellationTokenSource = new CancellationTokenSource();

        _disposables = new CompositeDisposable();
        _gameProgressSubscriber.Subscribe(evt =>
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
        _stats?.Reset(0);
        _jellyReward = 0;
        ClearSlow();

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



    public virtual void Initialize(SplineContainer splineContainer, SlimeDataBase data, int health, int jellyReward)
    {
        if (_splineAnimate == null || data == null)
            return;

        _data = data;
        _jellyReward = jellyReward;
        _stats.Reset(health);
        ClearSlow();

        _splineAnimate.Container = splineContainer;
        _splineAnimate.MaxSpeed = data.BaseSpeed;
        _splineAnimate.NormalizedTime = 0f;

        if (!_gameOver)
            _splineAnimate.Play();
    }

    public void SetDepthBucket(int bucket)
    {
        if (_propertyBlock == null)
            return;

        for (int i = 0; i < _depthBucketRenderers.Length; i++)
        {
            var renderer = _depthBucketRenderers[i];
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(DepthBucketId, bucket);
            renderer.SetPropertyBlock(_propertyBlock);
        }
    }

    public virtual void TakeDamage(DamageInfo info)
    {
        int before = _stats.CurrentHealth.Value;
        _stats.TakeDamage(info.Damage);
        int dealt = before - _stats.CurrentHealth.Value;

        if (dealt > 0)
            _damageTextSpawner.Show(dealt, _damageTextAnchor.position, info.IsCritical);

        if (_stats.IsDead)
        {
            ClearSlow();
            _splineAnimate.Pause();
            _killedPublisher.Publish(new SlimeKilledEvent(_jellyReward));
            OnDiedAsync();
            return;
        }

        if (info.HasSlow)
            ApplySlow(info);
    }

    public void ManagedUpdate()
    {
        if (_slow.Tick(Time.deltaTime, SlowMinSpeedRatio))
            ApplySpeed();

        if (!_slow.IsActive)
            UnregisterSlowUpdate();
    }

    private float SlowMinSpeedRatio => _gameplayService?.Config != null ? _gameplayService.Config.SlowMinSpeedRatio : 0.25f;

    private void ApplySlow(DamageInfo info)
    {
        if (_data == null)
            return;

        float percent = info.SlowPercent;
        if (_data is BossSlimeData)
            percent *= _gameplayService?.Config != null ? _gameplayService.Config.BossSlowScale : 0.5f;

        _slow.Apply(info.SourceSpawnId, percent, info.SlowDuration, SlowMinSpeedRatio);
        ApplySpeed();

        if (!_slowRegistered)
        {
            _updateService.RegisterUpdatable(this);
            _slowRegistered = true;
        }
    }

    // SplineAnimate는 속도를 바꾸면 진행 시간을 다시 계산해서 위치가 튄다. 진행률을 보존해 둔다.
    private void ApplySpeed()
    {
        if (_splineAnimate == null || _data == null)
            return;

        float progress = _splineAnimate.NormalizedTime;
        _splineAnimate.MaxSpeed = _data.BaseSpeed * _slow.SpeedRatio;
        _splineAnimate.NormalizedTime = progress;
    }

    private void ClearSlow()
    {
        bool wasSlowed = _slow.IsActive;
        _slow.Clear();
        UnregisterSlowUpdate();

        if (wasSlowed)
            ApplySpeed();
    }

    private void UnregisterSlowUpdate()
    {
        if (!_slowRegistered)
            return;

        _updateService.UnregisterUpdatable(this);
        _slowRegistered = false;
    }



    private async void OnDiedAsync()
    {
        try
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(_dieAnimationDuration),
                cancellationToken: _cancellationTokenSource.Token);

            _poolService.Release(gameObject);
        }
        catch (OperationCanceledException)
        {
        }
    }

    protected virtual void OnReachedEnd()
    {
        _reachedEndPublisher.Publish(new SlimeReachedEndEvent(_data.LifeCost, _data.InstantGameOver));
        _poolService.Release(gameObject);
    }

    protected virtual void OnDestroy()
    {
        UnregisterSlowUpdate();
        _disposables?.Dispose();
        _disposables = null;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        _stats?.Dispose();
    }
}
