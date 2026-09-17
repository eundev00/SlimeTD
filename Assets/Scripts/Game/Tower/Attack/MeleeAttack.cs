using UnityEngine;

public class MeleeAttack : AttackBehaviourBase
{
    private readonly MeleeAttackData _data;
    private AttackEffect _effect;

    public MeleeAttack(MeleeAttackData data) : base(data)
    {
        _data = data;
    }

    public override void Initialize(ITowerContext context)
    {
        base.Initialize(context);

        _effect = context.Transform.GetComponentInChildren<AttackEffect>(true);
        if (_effect == null)
            Debug.Log("[MeleeAttack] AttackEffect가 없습니다.", context.Transform);
    }

    protected override void OnAttackStarted()
    {
        if (_effect != null)
            _effect.Play();
    }

    // 취소로 공격이 끊기면 이펙트가 남는다.
    protected override void OnAttackFinished()
    {
        if (_effect != null)
            _effect.Stop();
    }

    protected override void Apply(in TargetInfo target)
    {
        target.Slime.TakeDamage(_data.Damage);
    }
}
