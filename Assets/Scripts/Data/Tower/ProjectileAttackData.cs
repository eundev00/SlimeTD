using UnityEngine;

[CreateAssetMenu(fileName = "Attack_00", menuName = "SlimeTD/Attack/Projectile", order = 62)]
public class ProjectileAttackData : AttackBehaviourData
{
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private int _poolCapacity = 10;
    [SerializeField] private int _poolMaxSize = 50;

    public GameObject ProjectilePrefab => _projectilePrefab;
    public int PoolCapacity => _poolCapacity;
    public int PoolMaxSize => _poolMaxSize;

    public override IAttackBehaviour CreateBehaviour()
    {
        return new ProjectileAttack(this);
    }
}
