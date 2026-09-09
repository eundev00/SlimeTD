using System;
using Cysharp.Threading.Tasks;

public interface IGameplayService : IDisposable
{
    GameplayInfo Info { get; }
    GameConfig Config { get; }
    TowerSpawnConfig TowerConfig { get; }
    WaveTableData WaveTable { get; }

    UniTask InitializeAsync();
    bool TrySpendGold(int amount);
    void SetMaxWave(int maxWave);
}
