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

    public HunterCardGachaCard(int towerId, int gradeId, HunterCardGachaCardKind kind, int refundGold = 0)
    {
        TowerId = towerId;
        GradeId = gradeId;
        Kind = kind;
        RefundGold = refundGold;
    }
}
