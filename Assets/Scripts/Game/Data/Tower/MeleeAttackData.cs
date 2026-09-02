using UnityEngine;

[CreateAssetMenu(fileName = "Attack_00", menuName = "SlimeTD/Attack/Melee", order = 63)]
public class MeleeAttackData : AttackBehaviourData
{
    public override IAttackBehaviour CreateBehaviour()
    {
        return new MeleeAttack(this);
    }
}
