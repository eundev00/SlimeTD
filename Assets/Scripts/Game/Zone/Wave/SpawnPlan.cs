public readonly struct SpawnPlan
{
    public readonly SlimeDataBase SlimeData;
    public readonly int Health;
    public readonly int JellyReward;
    public readonly float SpawnInterval;

    public SpawnPlan(SlimeDataBase slimeData, int health, int jellyReward, float spawnInterval)
    {
        SlimeData = slimeData;
        Health = health;
        JellyReward = jellyReward;
        SpawnInterval = spawnInterval;
    }
}
