public class MeleeAttack : AttackBehaviourBase
{
    private readonly MeleeAttackData _data;

    public MeleeAttack(MeleeAttackData data) : base(data)
    {
        _data = data;
    }

    protected override void Apply(in TargetInfo target)
    {
        target.Slime.TakeDamage(_data.Damage);
    }
}
