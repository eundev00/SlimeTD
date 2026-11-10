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
            case SkillEffectType.Slow:
                return valueType == SkillValueType.Percent;

            case SkillEffectType.Attack:
                return true;

            default:
                return valueType == SkillValueType.Fixed;
        }
    }
}
