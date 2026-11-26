using Cysharp.Threading.Tasks;
using MessagePipe;
using Services.PoolService;
using Services.UpdateService;
using System;
using System.Collections.Generic;
using System.Threading;
using UniRx;
using UnityEngine;
using VContainer;

public class BaseTower : MonoBehaviour, IUpdatable, IPeriodicUpdatable, ITowerInteractionHandler, ITowerContext
{
    private const float TickInterval = 0.1f;
    private const float RotationLerpSpeed = 10f;

    private static int _nextSpawnId;

    [NotNull][SerializeField] private TowerRangeIndicator _rangeIndicator;
    [NotNull][SerializeField] private TowerAnimator _animator;
    [NotNull][SerializeField] private TowerAnimationEventListener _animationEventListener;

    private Collider[] _colliders;

    private TowerData _data;
    private TowerStats _stats;
    private ITargetFinder _targetFinder;
    private IAttackBehaviour _attack;
    private IUpdateSubscriptionService _updateService;
    private IGameObjectPoolService _poolService;
    private ISubscriber<GameProgressEvent> _gameProgressSubscriber;
    private ITowerLevelService _levelService;
    private IGameplayService _gameplayService;
    private IResourceLoadService _resourceLoadService;
    private IVfxPlayer _vfxPlayer;
    private TowerSkillController _skills;
    private readonly TowerSkillVfx _skillVfx = new TowerSkillVfx();
    private readonly List<TowerSkillData> _activeSkills = new List<TowerSkillData>();

    private CompositeDisposable _disposables;
    private CancellationTokenSource _attackCancellation;
    private bool _isAttacking;

    private bool _gameOver;
    private bool _attackRegistered;

    private readonly ReactiveProperty<bool> _isSelected = new ReactiveProperty<bool>(false);

    private TargetInfo _currentTarget;
    private TargetInfo _attackTarget;
    private bool _hasTarget;
    private Quaternion _aimRotation = Quaternion.identity;

    public TowerStats Stats => _stats;
    public TowerData Data => _data;

    // 데이터 ID(TowerData.Id)와 다르다. 소환과 합성으로 생길 때마다 새로 받는 게임 중 번호다.
    public int SpawnId { get; private set; }

    // 배치 구역 그리드 좌표. 배치할 때 TowerSpawner가 정한다
    public Vector2Int Cell { get; set; }

    Transform ITowerContext.Transform => transform;
    Vector3 ITowerContext.AimDirection => transform.forward;
    IGameObjectPoolService ITowerContext.Pool => _poolService;
    TowerAnimator ITowerContext.Animator => _animator;
    TowerSkillController ITowerContext.Skills => _skills;
    void ITowerContext.AddGold(int amount) => _gameplayService.AddSkillGold(amount, Cell);
    void ITowerContext.PlaySkillVfx(IReadOnlyList<TowerSkillData> skills) => _skillVfx.Play(skills);
    void ITowerContext.PlayTargetVfx(IReadOnlyList<TowerSkillData> skills, Vector3 position) => _skillVfx.PlayAt(skills, position, _vfxPlayer);

    public IReadOnlyReactiveProperty<bool> IsSelected => _isSelected;

    [Inject]
    public void Construct(
        IUpdateSubscriptionService updateService,
        IGameObjectPoolService poolService,
        ISubscriber<GameProgressEvent> gameProgressSubscriber,
        ITowerLevelService levelService,
        IGameplayService gameplayService,
        IResourceLoadService resourceLoadService,
        IVfxPlayer vfxPlayer)
    {
        _vfxPlayer = vfxPlayer;
        _updateService = updateService;
        _poolService = poolService;
        _gameProgressSubscriber = gameProgressSubscriber;
        _levelService = levelService;
        _gameplayService = gameplayService;
        _resourceLoadService = resourceLoadService;
    }


    private void Awake()
    {
        if (_animator == null)
            Debug.Log("[BaseTower] TowerAnimator가 없습니다.", this);

        _targetFinder = new ClosestTargetFinder();
        _stats = new TowerStats();
        _disposables = new CompositeDisposable();

        _stats.AttackRange
            .Subscribe(range =>
            {
                if (_rangeIndicator != null)
                    _rangeIndicator.UpdateRangeVisual(range);
            })
            .AddTo(_disposables);

        _stats.AttackSpeed
            .Subscribe(speed =>
            {
                if (_animator != null)
                    _animator.SetAttackSpeed(speed);
            })
            .AddTo(_disposables);
    }

    // level이 0이면 계정 레벨을 쓴다
    public void Initialize(TowerData data, int level = 0)
    {
        if (data == null)
            return;

        _data = data;
        SpawnId = ++_nextSpawnId;

        if (level <= 0)
            level = _levelService.GetLevel(data.Id);
        _gameplayService.SkillCatalog?.GetActiveSkills(data.Id, level, _activeSkills);

        _stats.Initialize(data, level, _activeSkills);
        _skills = new TowerSkillController(_activeSkills);
        _skillVfx.LoadAsync(_activeSkills, _resourceLoadService, GetComponent<TowerVfxAnchors>(), transform, this.GetCancellationTokenOnDestroy()).Forget();

        _animator.PlaySpawn();
    }

    private void Start()
    {
        ApplySelection();

        if (_data == null)
        {
            Debug.Log("[BaseTower] TowerData가 없습니다.", this);
            return;
        }

        _attack = AttackBehaviourFactory.Create(_data, transform);
        if (_attack == null)
        {
            Debug.Log("[BaseTower] 기본 공격을 만들지 못했습니다.", this);
            return;
        }

        _attackCancellation = new CancellationTokenSource();
        _attack.Initialize(this);

        if (_animationEventListener != null)
            _animationEventListener.SetAttack(_attack);

        ApplyAttackActive();

        _gameProgressSubscriber.Subscribe(evt =>
        {
            if (evt.EventType == GameProgressType.GameOver)
                StopAttacking();
        }).AddTo(_disposables);
    }

    private void OnDestroy()
    {
        StopAttacking();

        _updateService.UnregisterUpdatable(this);
        _updateService.UnregisterPeriodicUpdatable(this);

        _attackCancellation?.Cancel();
        _attackCancellation?.Dispose();
        _attackCancellation = null;

        if (_animationEventListener != null)
            _animationEventListener.SetAttack(null);

        _attack?.Dispose();
        _attack = null;

        _disposables?.Dispose();
        _disposables = null;

        _stats?.Dispose();

        _isSelected.Dispose();
    }

    private void StopAttacking()
    {
        _gameOver = true;
        ApplyAttackActive();
        _attackCancellation?.Cancel();
    }

    private void ApplyAttackActive()
    {
        bool shouldAttack = !_gameOver;
        if (shouldAttack == _attackRegistered)
            return;

        if (shouldAttack)
        {
            _updateService.RegisterPeriodicUpdatable(this, TickInterval);
            _updateService.RegisterUpdatable(this);
        }
        else
        {
            _updateService.UnregisterPeriodicUpdatable(this);
            _updateService.UnregisterUpdatable(this);
            _hasTarget = false;
        }

        _attackRegistered = shouldAttack;
    }



    public void ManagedPeriodicUpdate(float deltaTime)
    {
        if (_attack == null || _attackCancellation == null)
            return;

        _attack.Tick(TickInterval);

        _hasTarget = _targetFinder.TryFind(transform.position, _stats.AttackRange.Value, out _currentTarget);

        if (_isAttacking || !_attack.IsReady || !_hasTarget)
            return;

        _attackTarget = _currentTarget;

        TryGetAimRotation(_attackTarget, out _aimRotation);

        AttackAsync(_currentTarget).Forget();
    }

    public void ManagedUpdate()
    {
        if (_attack == null || !_attack.RequiresFacing)
            return;

        if (!_attack.IsAiming)
            return;

        if (_attackTarget.IsValid && TryGetAimRotation(_attackTarget, out var aimRotation))
            _aimRotation = aimRotation;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            _aimRotation,
            1f - Mathf.Exp(-RotationLerpSpeed * Time.deltaTime));
    }

    private async UniTaskVoid AttackAsync(TargetInfo target)
    {
        _isAttacking = true;

        try
        {
            await _attack.ExecuteAsync(target, _attackCancellation.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _isAttacking = false;
        }
    }

    private bool TryGetAimRotation(in TargetInfo target, out Quaternion rotation)
    {
        rotation = transform.rotation;

        if (target.Transform == null)
            return false;

        Vector3 horizontalDirection = target.Transform.position - transform.position;
        horizontalDirection.y = 0;

        if (horizontalDirection.sqrMagnitude <= Mathf.Epsilon)
            return false;

        rotation = Quaternion.LookRotation(horizontalDirection.normalized);
        return true;
    }

    public void Select()
    {
        if (_isSelected.Value)
            return;

        _isSelected.Value = true;
        ApplySelection();
    }

    public void Deselect()
    {
        if (!_isSelected.Value)
            return;

        _isSelected.Value = false;
        ApplySelection();
    }

    private void ApplySelection()
    {
        if (_rangeIndicator == null)
            return;

        if (_isSelected.Value)
        {
            _rangeIndicator.Show();
        }
        else
        {
            _rangeIndicator.Hide();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_stats == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _stats.AttackRange.Value);
    }
#endif
}
