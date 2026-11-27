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

public class BaseSlime : MonoBehaviour, ISlime, IPoolItem, IUpdatable, IFixedUpdatable
{
    // 사망·기절 연출은 일반 슬라임 몸(반지름 약 0.26) 크기에 맞춰 만들고, 큰 슬라임은 몸 크기 비율만큼 키운다
    private const float VfxBaseRadius = 0.2637f;
    private const float TicksPerSecond = 30f;

    [NotNull][SerializeField] private Transform _damageTextAnchor;
    [SerializeField] private GameObject _deathVfx;
    [SerializeField] private GameObject _stunVfx;
    [NotNull][SerializeField] private Collider _bodyCollider;

    private SplineAnimate _splineAnimate;
    private SlimeDrawOrder _drawOrder;
    private ISlimeDrawOrderService _drawOrderService;
    private SlimeStats _stats;
    private SlimeDataBase _data;
    private int _goldReward;
    private int _spawnNumber;
    private SplineContainer _container;
    private float _pathLength;
    private float _groundHeight;
    private float _localPerWorld;
    private float _distance;
    private float _previousDistance;
    private bool _moving;
    private IPublisher<SlimeKilledEvent> _killedPublisher;
    private IPublisher<SlimeReachedEndEvent> _reachedEndPublisher;
    private IGameObjectPoolService _poolService;
    private IVfxPlayer _vfxPlayer;
    private GameObject _stunVfxInstance;
    private ISubscriber<GameProgressEvent> _gameProgressSubscriber;
    private DamageTextSpawner _damageTextSpawner;
    private CancellationTokenSource _cancellationTokenSource;
    private CompositeDisposable _disposables;
    private bool _gameOver;

    private IUpdateSubscriptionService _updateService;
    private IGameplayService _gameplayService;
    private readonly SlimeSlow _slow = new SlimeSlow();
    private float _stunRemaining;
    private bool _updateRegistered;
    private ICoopBattleClock _serverClock;
    private int _spawnTick;

    public SlimeStats Stats => _stats;
    public Vector3 Position => transform.position;
    public Vector3 BodyCenter => _bodyCollider.bounds.center;

    [Inject]
    public void Construct(
        IPublisher<SlimeKilledEvent> killedPublisher,
        IPublisher<SlimeReachedEndEvent> reachedEndPublisher,
        IGameObjectPoolService poolService,
        ISubscriber<GameProgressEvent> gameProgressSubscriber,
        DamageTextSpawner damageTextSpawner,
        IUpdateSubscriptionService updateService,
        IGameplayService gameplayService,
        IVfxPlayer vfxPlayer,
        ISlimeDrawOrderService drawOrderService)
    {
        _drawOrderService = drawOrderService;
        _killedPublisher = killedPublisher;
        _reachedEndPublisher = reachedEndPublisher;
        _poolService = poolService;
        _gameProgressSubscriber = gameProgressSubscriber;
        _damageTextSpawner = damageTextSpawner;
        _updateService = updateService;
        _gameplayService = gameplayService;
        _vfxPlayer = vfxPlayer;
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

        _drawOrder = new SlimeDrawOrder(gameObject);

        _stats = new SlimeStats(0);

        // 이동은 서버 검증과 같은 틱(고정 업데이트)으로 직접 계산한다. 컴포넌트는 경로 참조용으로만 남긴다
        _splineAnimate.enabled = false;
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

    }

    public virtual void OnReturnToPool()
    {
        _stats?.Reset(0);
        _goldReward = 0;
        ClearSlow();
        ClearStun();

        _disposables?.Dispose();
        _disposables = null;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        _moving = false;
        _container = null;
        _serverClock = null;
        _drawOrderService?.Remove(_drawOrder);
        UnregisterUpdate();
    }

    private void StopMoving()
    {
        _gameOver = true;
        _moving = false;
    }



    public virtual void Initialize(SplineContainer splineContainer, float groundHeight, SlimeDataBase data, int health, int goldReward, int spawnNumber)
    {
        if (_splineAnimate == null || data == null)
            return;

        _groundHeight = groundHeight;

        _data = data;
        _goldReward = goldReward;
        _spawnNumber = spawnNumber;
        _stats.Reset(health);
        ClearSlow();
        ClearStun();

        _container = splineContainer;
        _pathLength = splineContainer.CalculateLength(0);
        float localLength = splineContainer.Splines[0].GetLength();
        _localPerWorld = _pathLength > 0f ? localLength / _pathLength : 1f;
        _distance = 0f;
        _previousDistance = 0f;
        ApplyPose(0f);
        _drawOrderService?.Add(_drawOrder, data.RenderGroup);

        _moving = !_gameOver;
        RegisterUpdate();
    }

    // 협동: 서버가 이동을 정한다. 위치는 나온 틱부터 지난 틱 × 속도이고, 도착은 서버 이벤트(RemoveByServer)로 끝난다
    public void FollowServerClock(ICoopBattleClock clock, int spawnTick)
    {
        _serverClock = clock;
        _spawnTick = spawnTick;
    }

    public void RemoveByServer()
    {
        _moving = false;
        _poolService.Release(gameObject);
    }

    public void ManagedFixedUpdate()
    {
        if (!_moving || _data == null || _serverClock != null)
            return;

        float dt = 1f / TicksPerSecond;
        _slow.Tick(dt, SlowMinSpeedRatio);

        _previousDistance = _distance;

        if (_stunRemaining > 0f)
        {
            _stunRemaining -= dt;
            if (_stunRemaining <= 0f)
            {
                _stunRemaining = 0f;
                SetStunVfx(false);
            }
            return;
        }

        _distance += _data.BaseSpeed * _slow.SpeedRatio / TicksPerSecond;
        if (_distance >= _pathLength)
        {
            _moving = false;
            OnReachedEnd();
        }
    }

    // 틱 사이는 직전 틱 위치와 보간해서 그린다
    public void ManagedUpdate()
    {
        if (!_moving)
            return;

        if (_serverClock != null)
        {
            float elapsedTicks = Mathf.Max(0f, _serverClock.Tick - _spawnTick);
            ApplyPose(elapsedTicks * _data.BaseSpeed / TicksPerSecond);
            return;
        }

        float alpha = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
        ApplyPose(Mathf.Lerp(_previousDistance, _distance, alpha));
    }

    private void ApplyPose(float distance)
    {
        if (_container == null)
            return;

        float t = _container.Splines[0].ConvertIndexUnit(Mathf.Min(distance, _pathLength) * _localPerWorld, PathIndexUnit.Distance, PathIndexUnit.Normalized);
        if (!_container.Evaluate(0, t, out var position, out var tangent, out var up))
            return;

        // 높이는 스플라인이 아니라 경로의 땅 표면이다. 슬라임 메시 바닥이 피벗이라 크기·스케일과 상관없이 땅에 선다
        transform.position = new Vector3(position.x, _groundHeight, position.z);

        // 직선 경로는 탄젠트가 0이라 노트 위(시작점 포함)에서 방향이 0이 된다. 조금 옆 지점의 방향을 쓴다
        Vector3 forward = tangent;
        if (forward.sqrMagnitude <= 0.0001f)
            forward = _container.EvaluateTangent(0, t < 1f ? Mathf.Min(1f, t + 0.01f) : t - 0.01f);

        if (forward.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(forward, up);
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
            _killedPublisher.Publish(new SlimeKilledEvent(_spawnNumber, _goldReward));
            Die();
            return;
        }

        if (info.HasSlow)
            ApplySlow(info);

        if (info.HasStun)
            ApplyStun(info.StunDuration);
    }

    // 협동: 피해와 처치는 서버가 정한다. 처치 골드도 서버 이벤트로 오므로 처치 알림을 내지 않는다
    public void ApplyServerDamage(int damage, bool critical, int health)
    {
        if (damage > 0)
            _damageTextSpawner.Show(damage, _damageTextAnchor.position, critical);

        _stats.SetHealth(health);
        if (_stats.IsDead)
            Die();
    }

    private void Die()
    {
        ClearSlow();
        ClearStun();
        _moving = false;
        // 죽는 모션 없이 바로 풀로 돌려보내고, 연기는 바닥에 가리지 않게 몸 가운데에서 따로 재생한다
        var body = _bodyCollider.bounds;
        _vfxPlayer?.Play(_deathVfx, body.center, body.extents.y / VfxBaseRadius);
        _poolService.Release(gameObject);
    }

    // 기절은 새로 걸리면 남은 시간과 비교해 긴 쪽으로 갱신한다.
    private void ApplyStun(float duration)
    {
        _stunRemaining = Mathf.Max(_stunRemaining, duration);
        SetStunVfx(true);
    }

    private void ClearStun()
    {
        _stunRemaining = 0f;
        SetStunVfx(false);
    }

    // 기절 표시는 슬라임을 따라다녀야 해서 자식으로 한 번만 만들어 두고 켜고 끈다. 머리 위 높이와 크기는 몸 충돌체로 정한다
    private void SetStunVfx(bool active)
    {
        if (_stunVfx == null || (!active && _stunVfxInstance == null))
            return;

        if (_stunVfxInstance == null)
        {
            var body = _bodyCollider.bounds;
            _stunVfxInstance = Instantiate(_stunVfx, transform);
            _stunVfxInstance.transform.position = new Vector3(body.center.x, body.max.y, body.center.z);
            _stunVfxInstance.transform.localScale = _stunVfx.transform.localScale * (body.extents.y / VfxBaseRadius);
        }

        if (_stunVfxInstance.activeSelf == active)
            return;

        _stunVfxInstance.SetActive(active);
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
    }

    private void ClearSlow()
    {
        _slow.Clear();
    }

    private void RegisterUpdate()
    {
        if (_updateRegistered)
            return;

        _updateService.RegisterUpdatable(this);
        _updateService.RegisterFixedUpdatable(this);
        _updateRegistered = true;
    }

    private void UnregisterUpdate()
    {
        if (!_updateRegistered)
            return;

        _updateService.UnregisterUpdatable(this);
        _updateService.UnregisterFixedUpdatable(this);
        _updateRegistered = false;
    }




    protected virtual void OnReachedEnd()
    {
        _reachedEndPublisher.Publish(new SlimeReachedEndEvent(_spawnNumber, _data.LifeCost, _data.InstantGameOver));
        _poolService.Release(gameObject);
    }

    protected virtual void OnDestroy()
    {
        _drawOrderService?.Remove(_drawOrder);
        _drawOrder?.Dispose();

        UnregisterUpdate();
        _disposables?.Dispose();
        _disposables = null;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        _stats?.Dispose();
    }
}
