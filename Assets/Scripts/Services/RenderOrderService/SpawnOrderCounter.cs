using UnityEngine;

public class SpawnOrderCounter : ISpawnOrderCounter
{
    private const int FallbackBucketCount = 16;

    private readonly IGameplayService _gameplayService;
    private int _counter;

    public SpawnOrderCounter(IGameplayService gameplayService)
    {
        _gameplayService = gameplayService;
    }

    private int BucketCount
    {
        get
        {
            var settings = _gameplayService?.DepthBucketSettings;
            return settings != null ? Mathf.Max(2, settings.BucketCount) : FallbackBucketCount;
        }
    }

    public int NextBucket(SlimeRenderGroup group)
    {
        int bucketCount = BucketCount;

        // 오프셋이 뒤로 미는 방향이라 작은 버킷일수록 앞에 그려진다.
        // 일반 슬라임은 1부터 쓰고, 보스는 0을 고정으로 받아 항상 맨 앞에 선다.
        if (group == SlimeRenderGroup.Boss)
            return 0;

        if (_counter >= bucketCount)
            _counter = 0;

        return _counter++ + 1;
    }

    // TODO: 분열 구현 시 부모와 겹치지 않는 버킷을 고르도록 확장
    public int NextBucketFor(int parentBucket)
    {
        return NextBucket(SlimeRenderGroup.Normal);
    }

    public void Reset()
    {
        _counter = 0;
    }
}
