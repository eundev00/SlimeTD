using System;

public interface IGameplayService : IDisposable
{
    GameplayInfo Info { get; }

    bool TrySpendGold(int amount);
    void SetMaxWave(int maxWave);
}
