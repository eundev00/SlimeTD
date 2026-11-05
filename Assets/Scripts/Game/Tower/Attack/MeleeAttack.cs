public class MeleeAttack : AttackBehaviourBase
{
    protected override void Apply(in TargetInfo target, in AttackSkillRoll roll, in DamageInfo info)
    {
        target.Slime.TakeDamage(info);

        if (roll.SplashRadius > 0f && target.Transform != null)
        {
            SplashDamage.Apply(target.Transform.position, roll.SplashRadius, info, target.Slime);
        }
    }
}
