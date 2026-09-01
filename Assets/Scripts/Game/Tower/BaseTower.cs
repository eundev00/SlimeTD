using Cysharp.Threading.Tasks;
using MessagePipe;
using Services.PoolService;
using Services.UpdateService;
using System;
using System.Threading;
using UniRx;
using UnityEngine;
using VContainer;

public class BaseTower : MonoBehaviour, IPeriodicUpdatable, ITowerInteractionHandler, ITowerContext
{
    private const float TickInterval = 0.1f;

    [NotNull][SerializeField] private TowerRangeIndicator _rangeIndicator;
    [NotNull][SerializeField] private TowerAnimator _animator;
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

    // 게임오버는 비가역, 드래그는 가역이라 한 플래그로 겸하면 게임오버 후 드래그로 공격이 되살아난다.
    private bool _gameOver;
    private bool _dragged;
    private bool _attackRegistered;

    private readonly ReactiveProperty<bool> _isSelected = new ReactiveProperty<bool>(false);
    private readonly ReactiveProperty<bool> _isDragging = new ReactiveProperty<bool>(false);

    private Vector3 _originPosition;
    private Vector3 _towerBodyLocalPosition;

    public TowerStats Stats => _stats;

    Transform ITowerContext.Transform => transform;
    IGameObjectPoolService ITowerContext.Pool => _poolService;
    // 인터페이스로 넘어간 뒤에는 Unity fake-null을 못 걸러내므로 여기서 진짜 null로 정규화한다.
    TowerAnimator ITowerContext.Animator => _animator != null ? _animator : null;

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
    }

    // TowerSpawner가 Awake 직후 Start 이전에 호출한다. Start에서 데이터를 쓰려면 이 순서가 지켜져야 한다.
    public void Initialize(TowerData data)
    {
        if (data == null)
            return;

        _data = data;
        _stats.Initialize(data);

        if (_animator != null)
        {
            _animator.PlaySpawn();
        }
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

        // 진행 중인 공격을 먼저 끊어야 부품이 파괴된 뒤에 이어지지 않는다.
        _attackCancellation?.Cancel();
        _attackCancellation?.Dispose();
        _attackCancellation = null;

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

    // RegisterPeriodicUpdatable은 interval을 등록 시점에 고정한다. 쿨다운은 능력이 자체 타이머로 관리한다.
    private void ApplyAttackActive()
    {
        bool shouldAttack = !_gameOver && !_dragged;
        if (shouldAttack == _attackRegistered)
            return;

        if (shouldAttack)
        {
            _updateService?.RegisterPeriodicUpdatable(this, TickInterval);
        }
        else
        {
            _updateService?.UnregisterPeriodicUpdatable(this);
        }

        _attackRegistered = shouldAttack;
    }



    public void ManagedPeriodicUpdate(float deltaTime)
    {
        if (_attack == null || _attackCancellation == null)
            return;

        // UpdateSubscriptionService가 넘기는 deltaTime은 Timer와 Time.time을 빼는 계산이라 신뢰할 수 없다.
        _attack.Tick(TickInterval);

        if (_isAttacking || !_attack.IsReady)
            return;

        if (!_targetFinder.TryFind(transform.position, _stats.AttackRange.Value, out var target))
            return;

        if (_attack.RequiresFacing)
            FaceTarget(target);

        AttackAsync(target).Forget();
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
            // 타워 파괴 시 취소되면 무시
        }
        finally
        {
            _isAttacking = false;
        }
    }

    private void FaceTarget(in TargetInfo target)
    {
        Vector3 horizontalDirection = target.Transform.position - transform.position;
        horizontalDirection.y = 0;

        if (horizontalDirection.sqrMagnitude > Mathf.Epsilon)
        {
            transform.rotation = Quaternion.LookRotation(horizontalDirection.normalized);
        }
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

        if (_rangeIndicator != null)
        {
            _rangeIndicator.ResetColor();
        }
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
        ApplyLift(_isSelected.Value);

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
