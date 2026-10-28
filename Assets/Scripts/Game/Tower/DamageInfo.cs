public readonly struct DamageInfo
{
    public readonly int Damage;
    public readonly bool IsCritical;

    // 같은 타워의 감속은 갱신하고 다른 타워의 감속은 중첩하려고 보낸 타워를 구분한다.
    public readonly int SourceSpawnId;
    public readonly float SlowPercent;
    public readonly float SlowDuration;

    public DamageInfo(int damage, bool isCritical)
        : this(damage, isCritical, 0, 0f, 0f)
    {
    }

    private DamageInfo(int damage, bool isCritical, int sourceSpawnId, float slowPercent, float slowDuration)
    {
        Damage = damage;
        IsCritical = isCritical;
        SourceSpawnId = sourceSpawnId;
        SlowPercent = slowPercent;
        SlowDuration = slowDuration;
    }

    public bool HasSlow => SlowPercent > 0f && SlowDuration > 0f;

    public DamageInfo WithSlow(int sourceSpawnId, float slowPercent, float slowDuration)
    {
        return new DamageInfo(Damage, IsCritical, sourceSpawnId, slowPercent, slowDuration);
    }
}
