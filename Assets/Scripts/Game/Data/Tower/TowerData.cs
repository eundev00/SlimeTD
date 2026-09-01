using UnityEngine;

[CreateAssetMenu(fileName = "TowerData_00", menuName = "SlimeTD/Tower Data", order = 61)]
public class TowerData : ScriptableObject
{
    [SerializeField] private GameObject _prefab;
    [SerializeField] private string _idleState = "Idle";
    [SerializeField] private float _attackRange = 5f;
    [SerializeField] private AttackBehaviourData _basicAttack;

    public GameObject Prefab => _prefab;
    public string IdleState => _idleState;
    public float AttackRange => _attackRange;
    public AttackBehaviourData BasicAttack => _basicAttack;
}
