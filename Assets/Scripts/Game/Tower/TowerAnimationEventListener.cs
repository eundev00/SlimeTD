using UnityEngine;

public class TowerAnimationEventListener : MonoBehaviour
{
    private IAttackBehaviour _attack;

    public void SetAttack(IAttackBehaviour attack)
    {
        _attack = attack;
    }

    public void OnAttackCast()
    {
        if (_attack == null)
            return;

        _attack.OnAnimationCast();
    }

    public void OnAttackHit()
    {
        if (_attack == null)
            return;

        _attack.OnAnimationHit();
    }

    public void OnAttackEnd()
    {
        if (_attack == null)
            return;

        _attack.OnAnimationEnd();
    }
}
