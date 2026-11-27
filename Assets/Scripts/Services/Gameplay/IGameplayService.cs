using System;
using Cysharp.Threading.Tasks;

public interface IGameplayService : IDisposable
{
    GameplayInfo Info { get; }
    GameConfig Config { get; }
    TowerCatalog Catalog { get; }
    SkillCatalog SkillCatalog { get; }
    StageData Stage { get; }

    UniTask InitializeAsync(string stageKey = DataKeys.Stage_SingleForest01);
    void AddSkillGold(int amount, UnityEngine.Vector2Int cell);
    bool TrySpendGold(int amount);
    bool TrySpendSummonCost();
    void SetMaxWave(int maxWave);
}
