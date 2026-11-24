using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SlimeTD.Protocol;
using UniRx;

public interface IAccountService
{
    bool IsInitialized { get; }
    IReadOnlyReactiveProperty<int> Coin { get; }
    IReadOnlyReactiveProperty<int> Diamond { get; }
    IReadOnlyReactiveProperty<int> BestWave { get; }
    IReadOnlyList<HunterState> Hunters { get; }
    int HunterCardGachaPity { get; }

    // 서버 접속, 로그인, 계정 받기까지 한다. 실패하면 false
    UniTask<bool> InitializeAsync();
    bool TryGetHunter(int towerId, out HunterState hunter);
    bool IsUnlocked(int towerId);
    bool IsAvailable(int towerId);
    void UnlockHunter(int towerId);

    UniTask<ResultCode> PurchaseHunterAsync(int towerId);

    void AddCoin(int amount);
    void AddDiamond(int amount);
    void AddHunterCards(int towerId, int amount);
    void SetBestWave(int wave);
    void ApplyItems(IEnumerable<ItemStack> items);
    bool TryGetLevelUpCost(int towerId, out int cards, out int coin);
    bool CanLevelUp(int towerId);
    UniTask<ResultCode> LevelUpAsync(int towerId);
    UniTask<(ResultCode Result, IReadOnlyList<HunterCardGachaCard> Cards)> DrawHunterCardGachaAsync(int packId);
}