public class TowerStats
{
    public float AttackRange { get; }
    public float AttackCooldown { get; }
    public int Damage { get; }

    public TowerStats(float attackRange, float attackCooldown, int damage)
    {
        AttackRange = attackRange;
        AttackCooldown = attackCooldown;
        Damage = damage;
    }
}
