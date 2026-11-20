using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniRx;

public interface IPlayerDataService
{
    bool IsInitialized { get; }
    IReadOnlyReactiveProperty<int> Coin { get; }
    IReadOnlyReactiveProperty<int> Diamond { get; }
    IReadOnlyReactiveProperty<int> BestWave { get; }
    IReadOnlyList<HunterState> Hunters { get; }
    int HunterCardGachaPity { get; }

    UniTask InitializeAsync();
    bool TryGetHunter(int towerId, out HunterState hunter);
    bool IsUnlocked(int towerId);
    bool IsAvailable(int towerId);
    void UnlockHunter(int towerId);

    bool TryPurchaseHunter(int towerId);

    void AddCoin(int amount);
    void AddDiamond(int amount);
    void AddHunterCards(int towerId, int amount);
    void ReportWaveReached(int wave);
    bool TryGetLevelUpCost(int towerId, out int cards, out int coin);
    bool CanLevelUp(int towerId);
    bool TryLevelUp(int towerId);
    bool TryApplyHunterCardGachaResult(HunterCardGachaResult result);
}