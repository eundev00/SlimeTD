using System;
using Cysharp.Threading.Tasks;

public interface IGameplayService : IDisposable
{
    GameplayInfo Info { get; }
    GameConfig Config { get; }
    TowerCatalog Catalog { get; }
    WaveTableData WaveTable { get; }
    SlimeDepthBucketSettings DepthBucketSettings { get; }

    UniTask InitializeAsync();
    bool TrySpendJelly(int amount);
    bool TrySpendSummonCost();
    void SetMaxWave(int maxWave);
}
