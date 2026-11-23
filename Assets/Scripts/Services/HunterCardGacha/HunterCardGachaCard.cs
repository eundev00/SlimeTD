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
    public readonly int RefundCoin;
    public readonly bool IsPity;

    public HunterCardGachaCard(int towerId, int gradeId, HunterCardGachaCardKind kind, int refundCoin = 0, bool isPity = false)
    {
        TowerId = towerId;
        GradeId = gradeId;
        Kind = kind;
        RefundCoin = refundCoin;
        IsPity = isPity;
    }
}
