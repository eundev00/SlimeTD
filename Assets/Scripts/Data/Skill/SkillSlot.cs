public readonly struct SkillSlot
{
    public readonly TowerSkillData Root;
    public readonly TowerSkillData Current;
    public readonly TowerSkillData Next;

    public SkillSlot(TowerSkillData root, TowerSkillData current, TowerSkillData next)
    {
        Root = root;
        Current = current;
        Next = next;
    }

    public bool IsLocked => Current == null;
    public bool HasNextUpgrade => Current != null && Next != null;
}
