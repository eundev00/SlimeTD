using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SlimeTD.Protocol;
using UniRx;

// TowerTestScene은 서버 없이 바로 실행하므로 모든 헌터를 쓸 수 있는 빈 계정으로 대신한다
public class TowerTestAccountService : IAccountService
{
    private readonly ReactiveProperty<int> _coin = new ReactiveProperty<int>();
    private readonly ReactiveProperty<int> _diamond = new ReactiveProperty<int>();
    private readonly ReactiveProperty<int> _bestWave = new ReactiveProperty<int>();
    private readonly List<HunterState> _hunters = new List<HunterState>();

    public bool IsInitialized => false;
    public IReadOnlyReactiveProperty<int> Coin => _coin;
    public IReadOnlyReactiveProperty<int> Diamond => _diamond;
    public IReadOnlyReactiveProperty<int> BestWave => _bestWave;
    public IReadOnlyList<HunterState> Hunters => _hunters;
    public int HunterCardGachaPity => 0;

    public UniTask<bool> InitializeAsync() => UniTask.FromResult(true);

    public bool TryGetHunter(int towerId, out HunterState hunter)
    {
        hunter = null;
        return false;
    }

    public bool IsUnlocked(int towerId) => true;
    public bool IsAvailable(int towerId) => true;
    public void UnlockHunter(int towerId) { }
    public UniTask<ResultCode> PurchaseHunterAsync(int towerId) => UniTask.FromResult(ResultCode.InvalidRequest);
    public void AddCoin(int amount) { }
    public void AddDiamond(int amount) { }
    public void AddHunterCards(int towerId, int amount) { }
    public void ReportWaveReached(int wave) { }

    public bool TryGetLevelUpCost(int towerId, out int cards, out int coin)
    {
        cards = 0;
        coin = 0;
        return false;
    }

    public bool CanLevelUp(int towerId) => false;
    public UniTask<ResultCode> LevelUpAsync(int towerId) => UniTask.FromResult(ResultCode.InvalidRequest);
    public UniTask<(ResultCode Result, IReadOnlyList<HunterCardGachaCard> Cards)> DrawHunterCardGachaAsync(int packId) =>
        UniTask.FromResult<(ResultCode, IReadOnlyList<HunterCardGachaCard>)>((ResultCode.InvalidRequest, null));
}