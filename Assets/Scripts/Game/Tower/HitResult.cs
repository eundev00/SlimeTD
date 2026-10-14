public readonly struct HitResult
{
    public readonly int Damage;
    public readonly bool IsCritical;

    public HitResult(int damage, bool isCritical)
    {
        Damage = damage;
        IsCritical = isCritical;
    }
}