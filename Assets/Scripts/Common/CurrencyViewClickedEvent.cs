public readonly struct CurrencyViewClickedEvent
{
    public readonly CurrencyType Type;

    public CurrencyViewClickedEvent(CurrencyType type)
    {
        Type = type;
    }
}
