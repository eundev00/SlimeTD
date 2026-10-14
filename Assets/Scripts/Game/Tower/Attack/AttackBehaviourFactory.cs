using UnityEngine;

public static class AttackBehaviourFactory
{
    public static IAttackBehaviour Create(TowerAttackSetting setting)
    {
        switch (setting.Type)
        {
            case TowerAttackType.Melee:
                return new MeleeAttack(setting);

            case TowerAttackType.Projectile:
                return new ProjectileAttack(setting);

            default:
                Debug.Log($"[AttackBehaviourFactory] 지원하지 않는 공격 타입입니다: {setting.Type}");
                return null;
        }
    }
}
