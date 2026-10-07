public readonly struct SlimeReachedEndEvent
{
    public readonly int LifeCost;
    public readonly bool InstantGameOver;

    public SlimeReachedEndEvent(int lifeCost, bool instantGameOver)
    {
        LifeCost = lifeCost;
        InstantGameOver = instantGameOver;
    }
}
