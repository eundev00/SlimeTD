public readonly struct AttackSkillRoll
{
    // 스킬이 발동한 공격은 종류와 상관없이 치명타 판정을 하지 않는다 (공격 1회에 스킬 하나만 발동)
    public readonly bool HasProc;
    public readonly int ExtraShots;
    public readonly float DamageMultiplier;
    public readonly float SplashRadius;
    public readonly int Gold;
    public readonly float StunDuration;
    public readonly int CrossRange;

    public AttackSkillRoll(bool hasProc, int extraShots, float damageMultiplier, float splashRadius, int gold, float stunDuration, int crossRange)
    {
        HasProc = hasProc;
        ExtraShots = extraShots;
        DamageMultiplier = damageMultiplier;
        SplashRadius = splashRadius;
        Gold = gold;
        StunDuration = stunDuration;
        CrossRange = crossRange;
    }
}
