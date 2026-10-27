public enum SkillTrigger
{
    Always,
    OnAttack,
    OnHit,
    OnKill,
    EveryNthAttack,
}

public enum SkillEffectType
{
    CritChance,
    CritBonus,
    AttackSpeed,
    Attack,
    MultiShot,
    Pierce,
    Slow,
    JellyGain,
    DamageMultiplier,
    Splash,
}

public enum SkillValueType
{
    Fixed,
    Percent,
}

public static class SkillEffectRules
{
    public static bool IsValueTypeAllowed(SkillEffectType effectType, SkillValueType valueType)
    {
        switch (effectType)
        {
            case SkillEffectType.AttackSpeed:
            case SkillEffectType.DamageMultiplier:
                return valueType == SkillValueType.Percent;

            case SkillEffectType.Attack:
            case SkillEffectType.Slow:
                return true;

            default:
                return valueType == SkillValueType.Fixed;
        }
    }
}
