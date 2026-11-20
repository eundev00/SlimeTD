public readonly struct SpawnPlan
{
    public readonly SlimeDataBase SlimeData;
    public readonly int Health;
    public readonly int GoldReward;
    public readonly float DelayBefore;
    public readonly float SpawnInterval;

    public SpawnPlan(SlimeDataBase slimeData, int health, int goldReward, float delayBefore, float spawnInterval)
    {
        SlimeData = slimeData;
        Health = health;
        GoldReward = goldReward;
        DelayBefore = delayBefore;
        SpawnInterval = spawnInterval;
    }
}
