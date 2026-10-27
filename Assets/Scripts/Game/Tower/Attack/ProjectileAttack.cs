using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class ProjectileAttack : AttackBehaviourBase
{
    private const float ExtraShotInterval = 0.15f;

    // 풀은 투사체 프리팹 단위로 하나만 만들어져서 타워별로 다른 값을 둘 수 없다.
    private const int PoolCapacity = 10;
    private const int PoolMaxSize = 20;

    private readonly GameObject _projectilePrefab;
    private readonly CancellationTokenSource _disposeCancellation = new CancellationTokenSource();
    private ProjectileLauncher _launcher;

    public ProjectileAttack(GameObject projectilePrefab)
    {
        _projectilePrefab = projectilePrefab;
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

    protected override void Apply(in TargetInfo target)
    {
        Fire(target);

        int extra = Context.Skills?.RollExtraShots() ?? 0;
        if (extra > 0)
            FireExtraAsync(target, extra).Forget();
    }

    private async UniTaskVoid FireExtraAsync(TargetInfo target, int count)
    {
        for (int i = 0; i < count; i++)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(ExtraShotInterval), cancellationToken: _disposeCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!target.IsValid)
                return;

            Fire(target);
        }
    }

    private void Fire(in TargetInfo target)
    {
        if (_launcher == null || _projectilePrefab == null)
            return;

        var projectileObject = Context.Pool.Get(_projectilePrefab);
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

        projectile.Initialize(GetFireDirection(target, firePosition), RollDamage(), Context.Skills?.PierceCount ?? 0);
    }

    public override void Dispose()
    {
        _disposeCancellation.Cancel();
        _disposeCancellation.Dispose();
        base.Dispose();
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
