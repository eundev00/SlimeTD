using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SlimeTD.Protocol;
using UniRx;
using UnityEngine;

// 오프라인 테스트용: 서버 없이 실행할 때마다 새 계정으로 시작하고 저장하지 않는다
public class OfflineAccountService : IAccountService, IDisposable
{
    // 데이터 저장소 NewAccount.csv, GameConstants.csv(energy.max)와 같은 값
    private const int StartCoin = 10000;
    private const int StartDiamond = 10000;
    private const int StartEnergy = 30;

    private readonly IResourceLoadService _resourceLoadService;
    private readonly ITowerLevelService _towerLevelService;

    private readonly ReactiveProperty<int> _coin = new ReactiveProperty<int>();
    private readonly ReactiveProperty<int> _diamond = new ReactiveProperty<int>();
    private readonly ReactiveProperty<int> _packTicket = new ReactiveProperty<int>();
    private readonly ReactiveProperty<int> _energy = new ReactiveProperty<int>();
    private readonly ReactiveProperty<int> _energyMax = new ReactiveProperty<int>();
    private readonly ReactiveProperty<long> _energyRecoverAtMs = new ReactiveProperty<long>();
    private readonly ReactiveProperty<int> _bestWave = new ReactiveProperty<int>();
    private readonly List<HunterState> _hunters = new List<HunterState>();
    private readonly Dictionary<int, HunterState> _huntersById = new Dictionary<int, HunterState>();

    public bool IsInitialized { get; private set; }
    public IReadOnlyReactiveProperty<int> Coin => _coin;
    public IReadOnlyReactiveProperty<int> Diamond => _diamond;
    public IReadOnlyReactiveProperty<int> PackTicket => _packTicket;
    public IReadOnlyReactiveProperty<int> Energy => _energy;
    public IReadOnlyReactiveProperty<int> EnergyMax => _energyMax;
    public IReadOnlyReactiveProperty<long> EnergyRecoverAtMs => _energyRecoverAtMs;
    public long ServerNowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public IReadOnlyReactiveProperty<int> BestWave => _bestWave;
    public IReadOnlyList<HunterState> Hunters => _hunters;

    public OfflineAccountService(IResourceLoadService resourceLoadService, ITowerLevelService towerLevelService)
    {
        _resourceLoadService = resourceLoadService;
        _towerLevelService = towerLevelService;
    }

    public async UniTask<bool> InitializeAsync()
    {
        if (IsInitialized)
            return true;

        var catalog = await _resourceLoadService.LoadAsync<TowerCatalog>(DataKeys.TowerCatalog);
        if (catalog == null)
        {
            Debug.Log("[OfflineAccountService] 타워 데이터를 읽지 못했습니다.");
            return false;
        }

        foreach (var tower in catalog.Towers)
        {
            if (tower == null || _huntersById.ContainsKey(tower.Id))
                continue;

            // 서버 새 계정과 같이 다이아 가격이 없는 헌터만 열려 있다
            var state = new HunterState(tower, 1, tower.UnlockDiamondPrice <= 0, 0);
            _hunters.Add(state);
            _huntersById.Add(tower.Id, state);
            _towerLevelService.SetLevel(tower.Id, 1);
        }

        _coin.Value = StartCoin;
        _diamond.Value = StartDiamond;
        _energy.Value = StartEnergy;
        _energyMax.Value = StartEnergy;
        _energyRecoverAtMs.Value = 0;
        _bestWave.Value = 0;

        Debug.Log("[OfflineAccountService] 오프라인 모드: 새 계정으로 시작합니다 (저장하지 않음)");
        IsInitialized = true;
        return true;
    }

    public bool TryGetHunter(int towerId, out HunterState hunter)
    {
        return _huntersById.TryGetValue(towerId, out hunter);
    }

    public bool IsUnlocked(int towerId)
    {
        return _huntersById.TryGetValue(towerId, out var hunter) && hunter.Unlocked.Value;
    }

    public bool IsAvailable(int towerId)
    {
        return !IsInitialized || IsUnlocked(towerId);
    }

    public void UnlockHunter(int towerId)
    {
        if (_huntersById.TryGetValue(towerId, out var hunter))
            hunter.Unlocked.Value = true;
    }

    public UniTask<ResultCode> PurchaseHunterAsync(int towerId)
    {
        if (!_huntersById.TryGetValue(towerId, out var hunter) || hunter.Unlocked.Value)
            return UniTask.FromResult(ResultCode.InvalidRequest);

        int price = hunter.Data.UnlockDiamondPrice;
        if (_diamond.Value < price)
            return UniTask.FromResult(ResultCode.NotEnoughItems);

        _diamond.Value -= price;
        hunter.Unlocked.Value = true;
        return UniTask.FromResult(ResultCode.Ok);
    }

    public void AddCoin(int amount)
    {
        _coin.Value = Mathf.Max(0, _coin.Value + amount);
    }

    public void AddDiamond(int amount)
    {
        _diamond.Value = Mathf.Max(0, _diamond.Value + amount);
    }

    public void AddHunterCards(int towerId, int amount)
    {
        if (_huntersById.TryGetValue(towerId, out var hunter))
            hunter.Cards.Value = Mathf.Max(0, hunter.Cards.Value + amount);
    }

    public void SetBestWave(int wave)
    {
        _bestWave.Value = wave;
    }

    public void ApplyItems(IEnumerable<ItemStack> items) { }

    public void SetEnergyRecoverAt(long serverMs) { }

    public UniTask RefreshItemsAsync() => UniTask.CompletedTask;

    public bool TryGetLevelUpCost(int towerId, out int cards, out int coin)
    {
        cards = 0;
        coin = 0;

        if (!_huntersById.TryGetValue(towerId, out var hunter))
            return false;

        int level = hunter.Level.Value;
        if (level >= TowerLevelService.MaxLevel)
            return false;

        var rule = hunter.Data.LevelUpCost;
        cards = rule.GetCards(level);
        coin = rule.GetCoin(level);
        return true;
    }

    public bool CanLevelUp(int towerId)
    {
        if (!TryGetLevelUpCost(towerId, out int cards, out int coin))
            return false;

        var hunter = _huntersById[towerId];
        return hunter.Unlocked.Value && hunter.Cards.Value >= cards && _coin.Value >= coin;
    }

    public UniTask<ResultCode> LevelUpAsync(int towerId)
    {
        if (!CanLevelUp(towerId) || !TryGetLevelUpCost(towerId, out int cards, out int coin))
            return UniTask.FromResult(ResultCode.InvalidRequest);

        var hunter = _huntersById[towerId];
        hunter.Cards.Value -= cards;
        _coin.Value -= coin;
        hunter.Level.Value += 1;
        _towerLevelService.SetLevel(towerId, hunter.Level.Value);
        return UniTask.FromResult(ResultCode.Ok);
    }

    // TODO: 오프라인 뽑기는 아직 없다
    public UniTask<(ResultCode Result, IReadOnlyList<HunterCardGachaCard> Cards)> DrawHunterCardGachaAsync(int packId)
    {
        Debug.Log("[OfflineAccountService] 오프라인 모드에서는 헌터 카드 뽑기를 지원하지 않습니다.");
        return UniTask.FromResult<(ResultCode, IReadOnlyList<HunterCardGachaCard>)>((ResultCode.InvalidRequest, null));
    }

    public void Dispose()
    {
        foreach (var hunter in _hunters)
            hunter.Dispose();

        _hunters.Clear();
        _huntersById.Clear();
        _coin.Dispose();
        _diamond.Dispose();
        _packTicket.Dispose();
        _energy.Dispose();
        _energyMax.Dispose();
        _energyRecoverAtMs.Dispose();
        _bestWave.Dispose();
    }
}
