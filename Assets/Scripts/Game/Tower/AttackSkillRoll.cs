public readonly struct AttackSkillRoll
{
    public readonly int ExtraShots;
    public readonly float DamageMultiplier;
    public readonly float SplashRadius;
    public readonly int Jelly;
    public readonly float StunDuration;

    public AttackSkillRoll(int extraShots, float damageMultiplier, float splashRadius, int jelly, float stunDuration)
    {
        ExtraShots = extraShots;
        DamageMultiplier = damageMultiplier;
        SplashRadius = splashRadius;
        Jelly = jelly;
        StunDuration = stunDuration;
    }

    // 데미지에 영향을 주는 스킬이 발동한 공격은 치명타 판정을 하지 않는다. 젤리와 기절은 데미지와 무관하다.
    public bool HasDamageProc => ExtraShots > 0 || DamageMultiplier > 0f || SplashRadius > 0f;
}
