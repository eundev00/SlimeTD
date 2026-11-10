using UnityEngine;

public class ProjectileAttack : AttackBehaviourBase
{
    // 풀은 투사체 프리팹 단위로 하나만 만들어져서 타워별로 다른 값을 둘 수 없다.
    private const int PoolCapacity = 10;
    private const int PoolMaxSize = 20;

    private readonly GameObject _projectilePrefab;
    private readonly ProjectileStats _stats;
    private ProjectileLauncher _launcher;

    public ProjectileAttack(GameObject projectilePrefab, ProjectileStats stats)
    {
        _projectilePrefab = projectilePrefab;
        _stats = stats;
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

        if (_projectilePrefab == null)
        {
            Debug.Log("[ProjectileAttack] 발사체 프리팹이 없습니다.", context.Transform);
            return;
        }

        context.Pool.CreatePool(_projectilePrefab, PoolCapacity, PoolMaxSize);
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

    // 한 공격의 화살은 같은 데미지를 공유한다. 스킬이 발동했으면 치명타가 없으므로 값이 같아진다.
    protected override void Apply(in TargetInfo target, in AttackSkillRoll roll, in DamageInfo info)
    {
        if (roll.ExtraShots <= 0)
        {
            Fire(target, info, roll.SplashRadius);
            return;
        }

        FireSpread(target, roll.ExtraShots + 1, info, roll.SplashRadius);
    }

    private void Fire(in TargetInfo target, DamageInfo info, float splashRadius)
    {
        if (_launcher == null)
            return;

        Vector3 firePosition = _launcher.FirePoint.position;
        Launch(firePosition, GetFireDirection(target, firePosition), info, splashRadius);
    }

    // 여러 발은 발사 지점 좌우로 벌려 동시에 쏘고, 각자 대상 중심을 향하게 해서 모두 맞게 한다.
    private void FireSpread(in TargetInfo target, int count, DamageInfo info, float splashRadius)
    {
        if (_launcher == null)
            return;

        Vector3 center = _launcher.FirePoint.position;
        Vector3 aim = Context.AimDirection;
        Vector3 side = Vector3.Cross(Vector3.up, new Vector3(aim.x, 0f, aim.z)).normalized;
        float spacing = _launcher.MultiShotSpacing;

        for (int i = 0; i < count; i++)
        {
            float offset = (i - (count - 1) * 0.5f) * spacing;
            Vector3 firePosition = center + side * offset;
            Launch(firePosition, GetDirectionToTarget(target, firePosition, aim), info, splashRadius);
        }
    }

    private void Launch(Vector3 firePosition, Vector3 direction, DamageInfo info, float splashRadius)
    {
        if (_projectilePrefab == null)
            return;

        var projectileObject = Context.Pool.Get(_projectilePrefab);
        if (projectileObject == null)
            return;

        projectileObject.transform.position = firePosition;

        var projectile = projectileObject.GetComponent<Projectile>();
        if (projectile == null)
        {
            Debug.Log("[ProjectileAttack] 발사체 프리팹에 Projectile 컴포넌트가 없습니다.", projectileObject);
            return;
        }

        projectile.Initialize(direction, _stats, info, Context.Skills?.PierceCount ?? 0, splashRadius);
    }

    private Vector3 GetFireDirection(in TargetInfo target, Vector3 firePosition)
    {
        Vector3 aim = Context.AimDirection;
        Vector3 horizontal = new Vector3(aim.x, 0f, aim.z);

        if (target.Transform == null || horizontal.sqrMagnitude <= Mathf.Epsilon)
            return aim;

        Vector3 toTarget = GetTargetCenter(target) - firePosition;
        Vector3 toTargetHorizontal = new Vector3(toTarget.x, 0f, toTarget.z);

        return horizontal.normalized * toTargetHorizontal.magnitude + Vector3.up * toTarget.y;
    }

    private static Vector3 GetDirectionToTarget(in TargetInfo target, Vector3 firePosition, Vector3 fallback)
    {
        if (target.Transform == null)
            return fallback;

        Vector3 toTarget = GetTargetCenter(target) - firePosition;
        return toTarget.sqrMagnitude > Mathf.Epsilon ? toTarget : fallback;
    }

    private static Vector3 GetTargetCenter(in TargetInfo target)
    {
        return target.Transform.TryGetComponent<Collider>(out var collider)
            ? collider.bounds.center
            : target.Transform.position;
    }
}
