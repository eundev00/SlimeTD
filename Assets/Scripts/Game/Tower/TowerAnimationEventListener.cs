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

    // 공격은 OnAttackHit에서 끝난다. 클립의 OnAttackEnd는 모션 길이(시트 attackDuration을 뽑는 기준) 표시라 받기만 한다
    public void OnAttackEnd()
    {
    }
}
