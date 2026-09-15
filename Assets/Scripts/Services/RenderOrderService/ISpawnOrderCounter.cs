public interface ISpawnOrderCounter
{
    int NextBucket(SlimeRenderGroup group);
    int NextBucketFor(int parentBucket);
    void Reset();
}
