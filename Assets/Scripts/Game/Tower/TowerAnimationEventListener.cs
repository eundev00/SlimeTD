using UnityEngine;

public class TowerAnimationEventListener : MonoBehaviour
{
    private IAttackBehaviour _attack;

    public void SetAttack(IAttackBehaviour attack)
    {
        _attack = attack;
    }

    public void OnAttackHit()
    {
        _attack?.OnAnimationHit();
    }

    public void OnAttackEnd()
    {
        _attack?.OnAnimationEnd();
    }
}
