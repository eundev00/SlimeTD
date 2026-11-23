public readonly struct SlimeReachedEndEvent
{
    public readonly int SpawnNumber;
    public readonly int LifeCost;
    public readonly bool InstantGameOver;

    public SlimeReachedEndEvent(int spawnNumber, int lifeCost, bool instantGameOver)
    {
        SpawnNumber = spawnNumber;
        LifeCost = lifeCost;
        InstantGameOver = instantGameOver;
    }
}
