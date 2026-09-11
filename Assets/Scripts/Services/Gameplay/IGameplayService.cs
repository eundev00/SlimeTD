using System;
using Cysharp.Threading.Tasks;

public interface IGameplayService : IDisposable
{
    GameplayInfo Info { get; }
    GameConfig Config { get; }
    TowerTierTable TierTable { get; }
    WaveTableData WaveTable { get; }

    UniTask InitializeAsync();
    bool TrySpendGold(int amount);
    bool TrySpendSummonCost();
    void SetMaxWave(int maxWave);
}
