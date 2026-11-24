using System.Collections.Generic;

public class HunterCardGachaResult
{
    public int PackId { get; }
    public int Price { get; }
    public IReadOnlyList<HunterCardGachaCard> Cards { get; }

    public HunterCardGachaResult(int packId, int price, IReadOnlyList<HunterCardGachaCard> cards)
    {
        PackId = packId;
        Price = price;
        Cards = cards;
    }
}
