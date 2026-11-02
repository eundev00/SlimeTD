public readonly struct SkillTooltipContent
{
    public readonly string UnlockText;
    public readonly string Name;
    public readonly string Stage;
    public readonly string Description;
    public readonly string NextHeader;
    public readonly string NextName;
    public readonly string NextDescription;

    public SkillTooltipContent(string unlockText, string name, string stage, string description,
        string nextHeader, string nextName, string nextDescription)
    {
        UnlockText = unlockText;
        Name = name;
        Stage = stage;
        Description = description;
        NextHeader = nextHeader;
        NextName = nextName;
        NextDescription = nextDescription;
    }

    public bool HasNext => !string.IsNullOrEmpty(NextName);
}
