public enum HunterDetailActionMode
{
    LevelUp,
    MaxLevel,
    Unlock,
    UnlockUnavailable,
}

public readonly struct HunterDetailViewData
{
    public readonly string Name;
    public readonly int GradeId;
    public readonly string GradeName;
    public readonly string AttackTypeName;

    public readonly bool Unlocked;
    public readonly int Level;
    public readonly int MaxLevel;
    public readonly int OwnedCards;
    public readonly int NeededCards;
    public readonly bool CanLevelUp;

    public readonly string AttackValue;
    public readonly string AttackBonus;
    public readonly string AttackSpeed;

    public readonly HunterDetailActionMode Mode;
    public readonly bool ActionEnabled;
    public readonly string CostText;
    public readonly bool CostInsufficient;

    public readonly int Cards;
    public readonly int Gold;
    public readonly int Gem;

    public HunterDetailViewData(
        string name,
        int gradeId,
        string gradeName,
        string attackTypeName,
        bool unlocked,
        int level,
        int maxLevel,
        int ownedCards,
        int neededCards,
        bool canLevelUp,
        string attackValue,
        string attackBonus,
        string attackSpeed,
        HunterDetailActionMode mode,
        bool actionEnabled,
        string costText,
        bool costInsufficient,
        int cards,
        int gold,
        int gem)
    {
        Name = name;
        GradeId = gradeId;
        GradeName = gradeName;
        AttackTypeName = attackTypeName;
        Unlocked = unlocked;
        Level = level;
        MaxLevel = maxLevel;
        OwnedCards = ownedCards;
        NeededCards = neededCards;
        CanLevelUp = canLevelUp;
        AttackValue = attackValue;
        AttackBonus = attackBonus;
        AttackSpeed = attackSpeed;
        Mode = mode;
        ActionEnabled = actionEnabled;
        CostText = costText;
        CostInsufficient = costInsufficient;
        Cards = cards;
        Gold = gold;
        Gem = gem;
    }
}