using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniRx;

public interface IPlayerDataService
{
    bool IsInitialized { get; }
    IReadOnlyReactiveProperty<int> Gold { get; }
    IReadOnlyReactiveProperty<int> Gem { get; }
    IReadOnlyReactiveProperty<int> Cards { get; }
    IReadOnlyList<HunterState> Hunters { get; }

    UniTask InitializeAsync();
    bool TryGetHunter(int towerId, out HunterState hunter);
    bool IsUnlocked(int towerId);
    bool IsAvailable(int towerId);
    void UnlockHunter(int towerId);

    void AddGold(int amount);
    void AddCards(int amount);
    bool TryGetLevelUpCost(int towerId, out int cards, out int gold);
    bool CanLevelUp(int towerId);
    bool TryLevelUp(int towerId);
}