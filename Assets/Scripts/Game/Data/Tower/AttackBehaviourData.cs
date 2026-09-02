using UnityEngine;

public abstract class AttackBehaviourData : ScriptableObject
{
    [SerializeField] private int _damage = 1;
    [SerializeField] private float _cooldown = 1f;
    [SerializeField] private string _chargeState;
    [SerializeField] private string[] _attackStates;
    [SerializeField] private float _chargeDuration;
    [SerializeField] private float _attackDuration = 0.5f;

    public int Damage => _damage;
    public float Cooldown => _cooldown;
    public string ChargeState => _chargeState;
    public float ChargeDuration => _chargeDuration;
    public float AttackDuration => _attackDuration;

    // 여러 타워가 같은 에셋을 공유하므로 순환 인덱스는 여기가 아니라 부품 인스턴스가 갖는다.
    public string GetAttackState(int index)
    {
        if (_attackStates == null || _attackStates.Length == 0)
            return string.Empty;

        return _attackStates[index % _attackStates.Length];
    }

    public int GetNextAttackStateIndex(int index)
    {
        if (_attackStates == null || _attackStates.Length == 0)
            return 0;

        return (index + 1) % _attackStates.Length;
    }

    public abstract IAttackBehaviour CreateBehaviour();
}
