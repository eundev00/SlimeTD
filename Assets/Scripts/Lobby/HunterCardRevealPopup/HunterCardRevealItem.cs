using UnityEngine;

public readonly struct HunterCardRevealItem
{
    public readonly string Name;
    public readonly int GradeId;
    public readonly Sprite Thumbnail;

    public HunterCardRevealItem(string name, int gradeId, Sprite thumbnail)
    {
        Name = name;
        GradeId = gradeId;
        Thumbnail = thumbnail;
    }
}
