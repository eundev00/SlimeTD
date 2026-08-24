using MessagePipe;
using Services.PoolService;
using Services.UpdateService;
using UniRx;
using UnityEngine;
using VContainer;

public class BaseTower : MonoBehaviour, IPeriodicUpdatable, ITowerInteractionHandler
{
    [SerializeField] private float _attackRange = 5f;
    [SerializeField] private float _attackCooldown = 1f;
    [SerializeField] private int _damage = 1;
    [SerializeField] private Transform _firePoint;
    [NotNull][SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private TowerRangeIndicator _rangeIndicator;
    [SerializeField] private Transform _towerBody;
    [SerializeField] private float _liftHeight = 0.35f;

    private TowerStats _stats;
    private IUpdateSubscriptionService _updateService;
    private IGameObjectPoolService _poolService;
    private ISubscriber<GameProgressEvent> _gameProgressSubscriber;
    private LayerMask _slimeLayer;

    private CompositeDisposable _disposables;

    // 게임오버는 비가역, 드래그는 가역이라 한 플래그로 겸하면 게임오버 후 드래그로 공격이 되살아난다.
    private bool _gameOver;
    private bool _dragged;
    private bool _attackRegistered;

    private readonly ReactiveProperty<bool> _isSelected = new ReactiveProperty<bool>(false);
    private readonly ReactiveProperty<bool> _isDragging = new ReactiveProperty<bool>(false);

    private Vector3 _originPosition;
    private Vector3 _towerBodyLocalPosition;

    private readonly Collider[] _hitBuffer = new Collider[32];

    public TowerStats Stats => _stats;

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

    #region Unity Lifecycle

    private void Awake()
    {
        _slimeLayer = LayerMask.GetMask(GameTags.SlimeLayer);
        _stats = new TowerStats(_attackRange, _attackCooldown, _damage);

        ApplyRangeToIndicator();
    }

    private void ApplyRangeToIndicator()
    {
        if (_rangeIndicator == null)
        {
            Debug.LogWarning("[BaseTower] _rangeIndicator가 연결되지 않아 범위 표시를 갱신할 수 없습니다.", this);
            return;
        }

        _rangeIndicator.UpdateRangeVisual(_stats.AttackRange);
    }

    private void Start()
    {
        if (_towerBody != null)
        {
            _towerBodyLocalPosition = _towerBody.localPosition;
        }

        ApplySelection();

        if (_firePoint == null)
        {
            Debug.LogWarning("[BaseTower] _firePoint가 연결되지 않았습니다. 타워 위치에서 발사합니다.", this);
        }

        if (_projectilePrefab == null)
        {
            Debug.Log("[BaseTower] _projectilePrefab이 연결되지 않았습니다.", this);
            return;
        }

        _poolService.CreatePool(_projectilePrefab, 10, 50);
        ApplyAttackActive();

        _disposables = new CompositeDisposable();
        _gameProgressSubscriber.Subscribe(evt =>
        {
            if (evt.EventType == GameProgressType.GameOver)
                StopAttacking();
        }).AddTo(_disposables);
    }

    private void OnDestroy()
    {
        StopAttacking();
        _disposables?.Dispose();
        _disposables = null;

        _isSelected.Dispose();
        _isDragging.Dispose();
    }

    private void StopAttacking()
    {
        _gameOver = true;
        ApplyAttackActive();
    }

    private void ApplyAttackActive()
    {
        bool shouldAttack = !_gameOver && !_dragged;
        if (shouldAttack == _attackRegistered)
            return;

        if (shouldAttack)
        {
            _updateService?.RegisterPeriodicUpdatable(this, _stats.AttackCooldown);
        }
        else
        {
            _updateService?.UnregisterPeriodicUpdatable(this);
        }

        _attackRegistered = shouldAttack;
    }

    #endregion

    #region IPeriodicUpdatable

    public void ManagedPeriodicUpdate(float deltaTime)
    {
        var target = FindClosestSlime();
        if (target == null)
            return;

        FireProjectile(target);
    }

    #endregion

    private Transform FindClosestSlime()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, _stats.AttackRange, _hitBuffer, _slimeLayer);

        if (count == 0)
            return null;

        Transform closest = null;
        float closestSqrDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            if (!_hitBuffer[i].gameObject.activeInHierarchy)
                continue;

            float sqrDist = (_hitBuffer[i].transform.position - transform.position).sqrMagnitude;
            if (sqrDist < closestSqrDist)
            {
                closestSqrDist = sqrDist;
                closest = _hitBuffer[i].transform;
            }
        }

        return closest;
    }

    private void FireProjectile(Transform target)
    {
        // 타워 회전 (타워 본체 기준, 수평 방향만)
        Vector3 horizontalDirection = (target.position - transform.position).normalized;
        horizontalDirection.y = 0;

        if (horizontalDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(horizontalDirection);
        }

        // 총알 생성
        var projObj = _poolService.Get(_projectilePrefab);
        if (projObj == null)
            return;

        // 발사 위치 (_firePoint가 있으면 그 위치, 없으면 타워 본체)
        Vector3 firePosition = _firePoint != null ? _firePoint.position : transform.position;
        projObj.transform.position = firePosition;

        var projectile = projObj.GetComponent<Projectile>();
        if (projectile == null)
        {
            Debug.Log("[BaseTower] 발사체 프리팹에 Projectile 컴포넌트가 없습니다.", projObj);
            return;
        }

        // Projectile.Initialize가 발사 위치에서 타겟까지의 실제 3D 방향을 계산
        projectile.Initialize(target.position, _stats.Damage);
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
        float range = _stats != null ? _stats.AttackRange : _attackRange;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
#endif
}
