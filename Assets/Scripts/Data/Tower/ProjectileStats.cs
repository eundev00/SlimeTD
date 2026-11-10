public readonly struct ProjectileStats
{
    public readonly float Speed;
    public readonly float HitRadius;
    public readonly float Lifetime;

    public ProjectileStats(float speed, float hitRadius, float lifetime)
    {
        Speed = speed;
        HitRadius = hitRadius;
        Lifetime = lifetime;
    }
}
