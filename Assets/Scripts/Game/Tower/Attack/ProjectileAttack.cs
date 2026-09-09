using UnityEngine;

public class ProjectileAttack : AttackBehaviourBase
{
    private readonly ProjectileAttackData _data;
    private ProjectileLauncher _launcher;

    public ProjectileAttack(ProjectileAttackData data) : base(data)
    {
        _data = data;
    }

    public override void Initialize(ITowerContext context)
    {
        base.Initialize(context);

        _launcher = context.Transform.GetComponent<ProjectileLauncher>();
        if (_launcher == null)
        {
            Debug.Log("[ProjectileAttack] ProjectileLauncher가 없습니다.", context.Transform);
            return;
        }

        if (_data.ProjectilePrefab == null)
        {
            Debug.Log("[ProjectileAttack] 발사체 프리팹이 없습니다.", _data);
            return;
        }

        context.Pool.CreatePool(_data.ProjectilePrefab, _data.PoolCapacity, _data.PoolMaxSize);
    }

    protected override void OnAttackStarted()
    {
        if (_launcher != null)
            _launcher.SetHeldProjectileActive(true);
    }

    protected override void OnHitFrame()
    {
        if (_launcher != null)
            _launcher.SetHeldProjectileActive(false);
    }

    // 취소로 히트 프레임에 도달하지 못하면 손에 발사체가 남는다.
    protected override void OnAttackFinished()
    {
        if (_launcher != null)
            _launcher.SetHeldProjectileActive(false);
    }

    protected override void Apply(in TargetInfo target)
    {
        if (_launcher == null || _data.ProjectilePrefab == null)
            return;

        var projectileObject = Context.Pool.Get(_data.ProjectilePrefab);
        if (projectileObject == null)
            return;

        Vector3 firePosition = _launcher.FirePoint.position;
        projectileObject.transform.position = firePosition;

        var projectile = projectileObject.GetComponent<Projectile>();
        if (projectile == null)
        {
            Debug.Log("[ProjectileAttack] 발사체 프리팹에 Projectile 컴포넌트가 없습니다.", projectileObject);
            return;
        }

        projectile.Initialize(GetFireDirection(target, firePosition), _data.Damage);
    }

    private Vector3 GetFireDirection(in TargetInfo target, Vector3 firePosition)
    {
        Vector3 aim = Context.AimDirection;
        Vector3 horizontal = new Vector3(aim.x, 0f, aim.z);

        if (target.Transform == null || horizontal.sqrMagnitude <= Mathf.Epsilon)
            return aim;

        Vector3 targetCenter = target.Transform.TryGetComponent<Collider>(out var collider)
            ? collider.bounds.center
            : target.Transform.position;

        Vector3 toTarget = targetCenter - firePosition;
        Vector3 toTargetHorizontal = new Vector3(toTarget.x, 0f, toTarget.z);

        return horizontal.normalized * toTargetHorizontal.magnitude + Vector3.up * toTarget.y;
    }
}
