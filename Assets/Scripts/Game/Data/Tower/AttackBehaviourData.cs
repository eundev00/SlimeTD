using UnityEngine;

public abstract class AttackBehaviourData : ScriptableObject
{
    [SerializeField] private int _damage = 1;
    [SerializeField] private float _cooldown = 1f;
    [SerializeField] private string _chargeState;
    [SerializeField] private string _attackState;
    [SerializeField] private float _chargeDuration;
    [SerializeField] private float _attackDuration = 0.5f;

    public int Damage => _damage;
    public float Cooldown => _cooldown;
    public string ChargeState => _chargeState;
    public string AttackState => _attackState;
    public float ChargeDuration => _chargeDuration;
    public float AttackDuration => _attackDuration;

    public abstract IAttackBehaviour CreateBehaviour();
}
