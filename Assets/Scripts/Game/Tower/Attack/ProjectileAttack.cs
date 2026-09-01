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

    protected override void OnChargeStarted()
    {
        if (_launcher != null)
            _launcher.SetHeldProjectileActive(true);
    }

    protected override void OnChargeEnded()
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

        projectileObject.transform.position = _launcher.FirePoint.position;

        var projectile = projectileObject.GetComponent<Projectile>();
        if (projectile == null)
        {
            Debug.Log("[ProjectileAttack] 발사체 프리팹에 Projectile 컴포넌트가 없습니다.", projectileObject);
            return;
        }

        projectile.Initialize(target.Transform.position, _data.Damage);
    }
}
