using Cysharp.Threading.Tasks;
using MessagePipe;
using Services.PoolService;
using Services.UpdateService;
using System;
using System.Threading;
using UniRx;
using UnityEngine;
using VContainer;

public class BaseTower : MonoBehaviour, IUpdatable, IPeriodicUpdatable, ITowerInteractionHandler, ITowerContext
{
    private const float TickInterval = 0.1f;
    private const float RotationLerpSpeed = 10f;

    [NotNull][SerializeField] private TowerRangeIndicator _rangeIndicator;
    [NotNull][SerializeField] private TowerAnimator _animator;
    [NotNull][SerializeField] private TowerAnimationEventListener _animationEventListener;
    [NotNull][SerializeField] private Transform _towerBody;
    [SerializeField] private float _liftHeight = 0.35f;

    private TowerData _data;
    private TowerStats _stats;
    private ITargetFinder _targetFinder;
    private IAttackBehaviour _attack;
    private IUpdateSubscriptionService _updateService;
    private IGameObjectPoolService _poolService;
    private ISubscriber<GameProgressEvent> _gameProgressSubscriber;

    private CompositeDisposable _disposables;
    private CancellationTokenSource _attackCancellation;
    private bool _isAttacking;

    private bool _gameOver;
    private bool _dragged;
    private bool _attackRegistered;

    private readonly ReactiveProperty<bool> _isSelected = new ReactiveProperty<bool>(false);
    private readonly ReactiveProperty<bool> _isDragging = new ReactiveProperty<bool>(false);

    private Vector3 _originPosition;
    private Vector3 _towerBodyLocalPosition;

    private TargetInfo _currentTarget;
    private TargetInfo _attackTarget;
    private bool _hasTarget;
    private Quaternion _aimRotation = Quaternion.identity;

    public TowerStats Stats => _stats;
    public TowerData Data => _data;

    Transform ITowerContext.Transform => transform;
    Vector3 ITowerContext.AimDirection => transform.forward;
    IGameObjectPoolService ITowerContext.Pool => _poolService;
    TowerAnimator ITowerContext.Animator => _animator;

    public IReadOnlyReactiveProperty<bool> IsSelected => _isSelected;
    public IReadOnlyReactiveProperty<bool> IsDragging => _isDragging;

    [Inject]
    public void Construct(
        IUpdateSubscriptionService updateService,
        IGameObjectPoolService poolService,
        ISubscriber<GameProgressEvent> gameProgressSubscriber)
    {
        _updateService = updateService;
        _poolService = poolService;
        _gameProgressSubscriber = gameProgressSubscriber;
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

    public void Initialize(TowerData data)
    {
        if (data == null)
            return;

        _data = data;
        _stats.Initialize(data);

        _animator.PlaySpawn();
    }

    private void Start()
    {
        if (_towerBody != null)
        {
            _towerBodyLocalPosition = _towerBody.localPosition;
        }

        ApplySelection();

        if (_data == null)
        {
            Debug.Log("[BaseTower] TowerData가 없습니다.", this);
            return;
        }

        if (_data.BasicAttack == null)
        {
            Debug.Log("[BaseTower] TowerData에 기본 공격이 없습니다.", this);
            return;
        }

        _attackCancellation = new CancellationTokenSource();
        _attack = _data.BasicAttack.CreateBehaviour();
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

        _updateService?.UnregisterUpdatable(this);
        _updateService?.UnregisterPeriodicUpdatable(this);

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
        _isDragging.Dispose();
    }

    private void StopAttacking()
    {
        _gameOver = true;
        ApplyAttackActive();
        _attackCancellation?.Cancel();
    }

    private void AbortAttack()
    {
        if (_gameOver || _attackCancellation == null)
            return;

        _attackCancellation.Cancel();
        _attackCancellation.Dispose();
        _attackCancellation = new CancellationTokenSource();

        _hasTarget = false;
        _attackTarget = default;
        _animator?.PlayIdle();
    }

    private void ApplyAttackActive()
    {
        bool shouldAttack = !_gameOver && !_dragged;
        if (shouldAttack == _attackRegistered)
            return;

        if (shouldAttack)
        {
            _updateService?.RegisterPeriodicUpdatable(this, TickInterval);
            _updateService?.RegisterUpdatable(this);
        }
        else
        {
            _updateService?.UnregisterPeriodicUpdatable(this);
            _updateService?.UnregisterUpdatable(this);
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

    public void BeginDrag()
    {
        if (_isDragging.Value)
            return;

        _originPosition = transform.position;
        _isDragging.Value = true;

        _dragged = true;
        ApplyAttackActive();
        AbortAttack();
        ApplyLift(true);

        if (_rangeIndicator != null)
        {
            _rangeIndicator.Show();
        }
    }

    public void UpdateDragPosition(Vector3 worldPosition, bool isValid)
    {
        if (!_isDragging.Value)
            return;

        transform.position = worldPosition;

        if (_rangeIndicator != null)
        {
            _rangeIndicator.SetValid(isValid);
        }
    }

    public void EndDrag(Vector3 snappedWorldPosition)
    {
        if (!_isDragging.Value)
            return;

        transform.position = snappedWorldPosition;
        FinishDrag();
    }

    public void CancelDrag()
    {
        if (!_isDragging.Value)
            return;

        transform.position = _originPosition;
        FinishDrag();
    }

    private void FinishDrag()
    {
        _isDragging.Value = false;

        _dragged = false;
        ApplyAttackActive();
        ApplyLift(false);

        if (_rangeIndicator != null)
        {
            _rangeIndicator.ResetColor();
        }

        if (!_isSelected.Value)
            _rangeIndicator?.Hide();
    }

    private void ApplyLift(bool lifted)
    {
        if (_towerBody == null)
            return;

        _towerBody.localPosition = lifted
            ? _towerBodyLocalPosition + Vector3.up * _liftHeight
            : _towerBodyLocalPosition;
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
