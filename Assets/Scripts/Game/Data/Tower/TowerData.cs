using UnityEngine;

[CreateAssetMenu(fileName = "TowerData_00", menuName = "SlimeTD/Tower Data", order = 61)]
public class TowerData : ScriptableObject
{
    [SerializeField] private GameObject _prefab;
    [SerializeField] private float _attackRange = 5f;
    [SerializeField] private AttackBehaviourData _basicAttack;

    public GameObject Prefab => _prefab;
    public float AttackRange => _attackRange;
    public AttackBehaviourData BasicAttack => _basicAttack;
}
