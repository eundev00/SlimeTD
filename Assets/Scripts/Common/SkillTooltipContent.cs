using System;
using System.Collections.Generic;

public readonly struct SkillTooltipStage
{
    public readonly string Header;
    public readonly string Name;
    public readonly string Description;

    public SkillTooltipStage(string header, string name, string description)
    {
        Header = header;
        Name = name;
        Description = description;
    }
}

public readonly struct SkillTooltipContent
{
    private static readonly SkillTooltipStage[] NoStages = Array.Empty<SkillTooltipStage>();

    public readonly string UnlockText;
    public readonly string Name;
    public readonly string Description;
    public readonly IReadOnlyList<SkillTooltipStage> Upcoming;

    public SkillTooltipContent(string unlockText, string name, string description, IReadOnlyList<SkillTooltipStage> upcoming)
    {
        UnlockText = unlockText;
        Name = name;
        Description = description;
        Upcoming = upcoming ?? NoStages;
    }
}
