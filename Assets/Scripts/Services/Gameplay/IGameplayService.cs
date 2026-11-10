using System;
using Cysharp.Threading.Tasks;

public interface IGameplayService : IDisposable
{
    GameplayInfo Info { get; }
    GameConfig Config { get; }
    TowerCatalog Catalog { get; }
    SkillCatalog SkillCatalog { get; }
    StageData Stage { get; }
    SlimeDepthBucketSettings DepthBucketSettings { get; }

    UniTask InitializeAsync();
    void AddJelly(int amount);
    bool TrySpendJelly(int amount);
    bool TrySpendSummonCost();
    void SetMaxWave(int maxWave);
}
