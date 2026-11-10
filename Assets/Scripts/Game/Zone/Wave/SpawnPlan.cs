public readonly struct SpawnPlan
{
    public readonly SlimeDataBase SlimeData;
    public readonly int Health;
    public readonly int JellyReward;
    public readonly float DelayBefore;
    public readonly float SpawnInterval;

    public SpawnPlan(SlimeDataBase slimeData, int health, int jellyReward, float delayBefore, float spawnInterval)
    {
        SlimeData = slimeData;
        Health = health;
        JellyReward = jellyReward;
        DelayBefore = delayBefore;
        SpawnInterval = spawnInterval;
    }
}
