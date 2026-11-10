using UnityEngine;

public static class AttackBehaviourFactory
{
    // 공격 방식과 투사체 수치는 데이터가, 투사체 모양(프리팹)은 타워 프리팹의 ProjectileLauncher가 정한다
    public static IAttackBehaviour Create(TowerData data, Transform towerRoot)
    {
        switch (data.AttackType)
        {
            case TowerAttackType.Melee:
                return new MeleeAttack();

            case TowerAttackType.Projectile:
                var launcher = towerRoot.GetComponent<ProjectileLauncher>();
                return new ProjectileAttack(launcher != null ? launcher.ProjectilePrefab : null, data.Projectile);

            case TowerAttackType.Beam:
                return new BeamAttack();

            default:
                Debug.Log($"[AttackBehaviourFactory] 지원하지 않는 공격 타입입니다: {data.AttackType}");
                return null;
        }
    }
}
