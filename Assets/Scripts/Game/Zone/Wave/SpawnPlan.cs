public readonly struct SpawnPlan
{
    public readonly SlimeDataBase SlimeData;
    public readonly int Health;
    public readonly int GoldReward;
    public readonly float SpawnInterval;

    public SpawnPlan(SlimeDataBase slimeData, int health, int goldReward, float spawnInterval)
    {
        SlimeData = slimeData;
        Health = health;
        GoldReward = goldReward;
        SpawnInterval = spawnInterval;
    }
}
