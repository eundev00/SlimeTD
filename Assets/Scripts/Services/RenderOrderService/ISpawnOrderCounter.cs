public interface ISpawnOrderCounter
{
    int NextBucket(SlimeRenderGroup group);
    void Reset();
}
