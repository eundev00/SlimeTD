public readonly struct CurrencyChipClickedEvent
{
    public readonly CurrencyType Type;

    public CurrencyChipClickedEvent(CurrencyType type)
    {
        Type = type;
    }
}
