public readonly struct SlimeKilledEvent
{
    public readonly int SpawnNumber;
    public readonly int GoldReward;

    public SlimeKilledEvent(int spawnNumber, int goldReward)
    {
        SpawnNumber = spawnNumber;
        GoldReward = goldReward;
    }
}
