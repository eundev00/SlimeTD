using UnityEngine;

[CreateAssetMenu(fileName = "TowerData_00", menuName = "SlimeTD/Tower Data", order = 61)]
public class TowerData : ScriptableObject
{
    private const float BaseAttackSpeedPercent = 100f;

    [SerializeField] private int _id;
    [SerializeField] private string _towerName;
    [SerializeField] private GameObject _prefab;
    [SerializeField] private float _attackRange = 5f;
    [SerializeField] private AttackBehaviourData _basicAttack;

    [Header("기본 수치 (레벨 1)")]
    [SerializeField] private float _baseAttack = 1f;
    [SerializeField] private float _attackGrowthRate = 5f;
    [SerializeField] private float _attackCooldown = 1f;
    [SerializeField] private float _critChance = 10f;
    [SerializeField] private float _critBonus = 100f;

    public int Id => _id;
    public string TowerName => _towerName;
    public GameObject Prefab => _prefab;
    public float AttackRange => _attackRange;
    public AttackBehaviourData BasicAttack => _basicAttack;
    public float BaseAttack => _baseAttack;
    public float AttackCooldown => _attackCooldown;
    public float CritChance => _critChance;
    public float CritBonus => _critBonus;
    public float AttackSpeedPercent => BaseAttackSpeedPercent;

    public float GetAttack(int level)
    {
        int steps = Mathf.Max(0, level - 1);
        return _baseAttack * (1f + _attackGrowthRate / 100f * steps);
    }
}
