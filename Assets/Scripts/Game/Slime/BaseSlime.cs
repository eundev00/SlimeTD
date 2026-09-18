using Cysharp.Threading.Tasks;
using MessagePipe;
using Services.PoolService;
using System;
using System.Threading;
using UniRx;
using UnityEngine;
using UnityEngine.Splines;
using VContainer;

public class BaseSlime : MonoBehaviour, ISlime, IPoolItem
{
    private static readonly int DepthBucketId = Shader.PropertyToID("_SlimeDepthBucket");

    [SerializeField] private float _dieAnimationDuration = 1f;
    [SerializeField] private SkinnedMeshRenderer[] _depthBucketRenderers;
    [NotNull][SerializeField] private Transform _damageTextAnchor;

    private SplineAnimate _splineAnimate;
    private MaterialPropertyBlock _propertyBlock;
    private SlimeStats _stats;
    private SlimeDataBase _data;
    private int _goldReward;
    private IPublisher<SlimeKilledEvent> _killedPublisher;
    private IPublisher<SlimeReachedEndEvent> _reachedEndPublisher;
    private IGameObjectPoolService _poolService;
    private ISubscriber<GameProgressEvent> _gameProgressSubscriber;
    private DamageTextSpawner _damageTextSpawner;
    private CancellationTokenSource _cancellationTokenSource;
    private CompositeDisposable _disposables;
    private bool _gameOver;

    public SlimeStats Stats => _stats;
    public Vector3 Position => transform.position;

    [Inject]
    public void Construct(
        IPublisher<SlimeKilledEvent> killedPublisher,
        IPublisher<SlimeReachedEndEvent> reachedEndPublisher,
        IGameObjectPoolService poolService,
        ISubscriber<GameProgressEvent> gameProgressSubscriber,
        DamageTextSpawner damageTextSpawner)
    {
        _killedPublisher = killedPublisher;
        _reachedEndPublisher = reachedEndPublisher;
        _poolService = poolService;
        _gameProgressSubscriber = gameProgressSubscriber;
        _damageTextSpawner = damageTextSpawner;
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
        _goldReward = 0;

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



    public virtual void Initialize(SplineContainer splineContainer, SlimeDataBase data, int health, int goldReward)
    {
        if (_splineAnimate == null || data == null)
            return;

        _data = data;
        _goldReward = goldReward;
        _stats.Reset(health);

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

    public virtual void TakeDamage(int damage)
    {
        int before = _stats.CurrentHealth.Value;
        _stats.TakeDamage(damage);
        int dealt = before - _stats.CurrentHealth.Value;

        if (dealt > 0)
            _damageTextSpawner.Show(dealt, _damageTextAnchor.position);

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

            _poolService.Release(gameObject);
        }
        catch (OperationCanceledException)
        {
        }
    }

    protected virtual void OnReachedEnd()
    {
        _reachedEndPublisher.Publish(new SlimeReachedEndEvent(_data.LifeCost));
        _poolService.Release(gameObject);
    }

    protected virtual void OnDestroy()
    {
        _disposables?.Dispose();
        _disposables = null;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        _stats?.Dispose();
    }
}
