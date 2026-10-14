using UnityEngine;

public static class AttackBehaviourFactory
{
    public static IAttackBehaviour Create(TowerData data)
    {
        switch (data.AttackType)
        {
            case TowerAttackType.Melee:
                return new MeleeAttack();

            case TowerAttackType.Projectile:
                return new ProjectileAttack(data.ProjectilePrefab);

            default:
                Debug.Log($"[AttackBehaviourFactory] 지원하지 않는 공격 타입입니다: {data.AttackType}");
                return null;
        }
    }
}
