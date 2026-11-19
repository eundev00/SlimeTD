public enum HunterCardGachaCardKind
{
    Card,
    Unlock,
    Refund,
}

public readonly struct HunterCardGachaCard
{
    public readonly int TowerId;
    public readonly int GradeId;
    public readonly HunterCardGachaCardKind Kind;
    public readonly int RefundGold;
    public readonly bool IsPity;

    public HunterCardGachaCard(int towerId, int gradeId, HunterCardGachaCardKind kind, int refundGold = 0, bool isPity = false)
    {
        TowerId = towerId;
        GradeId = gradeId;
        Kind = kind;
        RefundGold = refundGold;
        IsPity = isPity;
    }
}
