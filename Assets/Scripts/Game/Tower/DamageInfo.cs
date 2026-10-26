public readonly struct DamageInfo
{
    public readonly int Damage;
    public readonly bool IsCritical;

    public DamageInfo(int damage, bool isCritical)
    {
        Damage = damage;
        IsCritical = isCritical;
    }
}
