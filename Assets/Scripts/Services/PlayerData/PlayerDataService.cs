using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

public class PlayerDataService : IPlayerDataService, IDisposable
{
    private readonly IPlayerDataStore _store;
    private readonly IResourceLoadService _resourceLoadService;
    private readonly ITowerLevelService _towerLevelService;

    private readonly ReactiveProperty<int> _gold = new ReactiveProperty<int>();
    private readonly ReactiveProperty<int> _gem = new ReactiveProperty<int>();
    private readonly ReactiveProperty<int> _bestWave = new ReactiveProperty<int>();
    private readonly List<HunterState> _hunters = new List<HunterState>();
    private readonly Dictionary<int, HunterState> _huntersById = new Dictionary<int, HunterState>();

    public bool IsInitialized { get; private set; }
    public IReadOnlyReactiveProperty<int> Gold => _gold;
    public IReadOnlyReactiveProperty<int> Gem => _gem;
    public IReadOnlyReactiveProperty<int> BestWave => _bestWave;
    public IReadOnlyList<HunterState> Hunters => _hunters;
    public int HunterCardGachaPity { get; private set; }

    public PlayerDataService(
        IPlayerDataStore store,
        IResourceLoadService resourceLoadService,
        ITowerLevelService towerLevelService)
    {
        _store = store;
        _resourceLoadService = resourceLoadService;
        _towerLevelService = towerLevelService;
    }

    public async UniTask InitializeAsync()
    {
        if (IsInitialized)
            return;

        var catalog = await _resourceLoadService.LoadAsync<TowerCatalog>(DataKeys.TowerCatalog);
        if (catalog == null)
        {
            Debug.Log($"[PlayerDataService] {DataKeys.TowerCatalog} 로드에 실패했습니다.");
            return;
        }

        var config = await _resourceLoadService.LoadAsync<GameConfig>(DataKeys.GameConfig);
        if (config == null)
            Debug.Log($"[PlayerDataService] {DataKeys.GameConfig} 로드에 실패해 시작 재화를 0으로 둡니다.");

        var data = await _store.LoadAsync();
        bool isNew = data == null;
        if (isNew)
            data = CreateNewPlayerData(config);

        var recordsById = new Dictionary<int, HunterRecord>();
        foreach (var record in data.Hunters)
            recordsById[record.TowerId] = record;

        var towers = catalog.Towers;
        bool created = false;

        for (int i = 0; towers != null && i < towers.Length; i++)
        {
            var tower = towers[i];
            if (tower == null || _huntersById.ContainsKey(tower.Id))
                continue;

            if (!recordsById.TryGetValue(tower.Id, out var record))
            {
                record = new HunterRecord
                {
                    TowerId = tower.Id,
                    Level = TowerLevelService.MinLevel,
                    Unlocked = tower.StartUnlocked,
                    Cards = config != null ? config.InitialHunterCards : 0,
                };

                data.Hunters.Add(record);
                created = true;
            }

            var state = new HunterState(tower, record);
            _hunters.Add(state);
            _huntersById.Add(tower.Id, state);

            _towerLevelService.SetLevel(tower.Id, state.Level.Value);
        }

        _gold.Value = data.Gold;
        _gem.Value = data.Gem;
        _bestWave.Value = data.BestWave;
        HunterCardGachaPity = data.HunterCardGachaPity;
        IsInitialized = true;

        if (isNew || created)
            SaveAsync().Forget();
    }

    // 저장 데이터가 없는 새 계정은 GameConfig의 시작 재화로 시작한다. 헌터 카드는 헌터 기록을 만들 때 넣는다.
    private static PlayerData CreateNewPlayerData(GameConfig config)
    {
        var data = new PlayerData();
        if (config == null)
            return data;

        data.Gold = config.InitialLobbyGold;
        data.Gem = config.InitialGem;
        return data;
    }

    public bool TryGetHunter(int towerId, out HunterState hunter)
    {
        return _huntersById.TryGetValue(towerId, out hunter);
    }

    public bool IsUnlocked(int towerId)
    {
        return _huntersById.TryGetValue(towerId, out var hunter) && hunter.Unlocked.Value;
    }

    // 로비를 거치지 않고 게임 씬만 실행한 경우(에디터 테스트)에는 제한 없이 쓴다.
    public bool IsAvailable(int towerId)
    {
        return !IsInitialized || IsUnlocked(towerId);
    }

    public void UnlockHunter(int towerId)
    {
        if (!_huntersById.TryGetValue(towerId, out var hunter) || hunter.Unlocked.Value)
            return;

        hunter.Unlocked.Value = true;
        SaveAsync().Forget();
    }

    // 잠긴 헌터는 보석으로 구매해 해금한다. 가격이 0이면 구매할 수 없다.
    public bool TryPurchaseHunter(int towerId)
    {
        if (!_huntersById.TryGetValue(towerId, out var hunter) || hunter.Unlocked.Value)
            return false;

        int price = hunter.Data.UnlockGemPrice;
        if (price <= 0 || _gem.Value < price)
            return false;

        _gem.Value -= price;
        hunter.Unlocked.Value = true;
        SaveAsync().Forget();
        return true;
    }

    public void AddGold(int amount)
    {
        if (amount == 0)
            return;

        _gold.Value = Mathf.Max(0, _gold.Value + amount);
        SaveAsync().Forget();
    }

    public void AddGem(int amount)
    {
        if (amount == 0)
            return;

        _gem.Value = Mathf.Max(0, _gem.Value + amount);
        SaveAsync().Forget();
    }

    // 헌터 카드는 헌터마다 따로 쌓인다. 잠긴 헌터도 받을 수 있다 (이벤트 지급 등).
    public void AddHunterCards(int towerId, int amount)
    {
        if (amount == 0 || !_huntersById.TryGetValue(towerId, out var hunter))
            return;

        hunter.Cards.Value = Mathf.Max(0, hunter.Cards.Value + amount);
        SaveAsync().Forget();
    }

    // 초기화 전(게임 씬 단독 실행)에 저장하면 빈 헌터 목록으로 기존 저장이 덮인다
    public void ReportWaveReached(int wave)
    {
        if (!IsInitialized || wave <= _bestWave.Value)
            return;

        _bestWave.Value = wave;
        SaveAsync().Forget();
    }

    public bool TryGetLevelUpCost(int towerId, out int cards, out int gold)
    {
        cards = 0;
        gold = 0;

        if (!_huntersById.TryGetValue(towerId, out var hunter))
            return false;

        int level = hunter.Level.Value;
        if (level >= TowerLevelService.MaxLevel)
            return false;

        var rule = hunter.Data.LevelUpCost;
        cards = rule.GetCards(level);
        gold = rule.GetGold(level);
        return true;
    }

    public bool CanLevelUp(int towerId)
    {
        if (!TryGetLevelUpCost(towerId, out int cards, out int gold))
            return false;

        var hunter = _huntersById[towerId];
        return hunter.Unlocked.Value && hunter.Cards.Value >= cards && _gold.Value >= gold;
    }

    public bool TryLevelUp(int towerId)
    {
        if (!CanLevelUp(towerId))
            return false;

        TryGetLevelUpCost(towerId, out int cards, out int gold);

        var hunter = _huntersById[towerId];
        hunter.Cards.Value -= cards;
        _gold.Value -= gold;
        hunter.Level.Value += 1;

        _towerLevelService.SetLevel(towerId, hunter.Level.Value);
        SaveAsync().Forget();
        return true;
    }

    // 뽑기 한 번의 골드 차감, 카드 지급, 해금, 환급, 천장 카운터를 한꺼번에 반영하고 한 번만 저장한다.
    public bool TryApplyHunterCardGachaResult(HunterCardGachaResult result)
    {
        if (!IsInitialized || result == null || _gold.Value < result.Price)
            return false;

        _gold.Value -= result.Price;

        var cards = result.Cards;
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (!_huntersById.TryGetValue(card.TowerId, out var hunter))
                continue;

            switch (card.Kind)
            {
                case HunterCardGachaCardKind.Unlock:
                    hunter.Unlocked.Value = true;
                    break;
                case HunterCardGachaCardKind.Refund:
                    _gold.Value += card.RefundGold;
                    break;
                default:
                    hunter.Cards.Value += 1;
                    break;
            }
        }

        HunterCardGachaPity = result.PityAfter;
        SaveAsync().Forget();
        return true;
    }

    public void Dispose()
    {
        for (int i = 0; i < _hunters.Count; i++)
            _hunters[i].Dispose();

        _hunters.Clear();
        _huntersById.Clear();
        _gold.Dispose();
        _gem.Dispose();
        _bestWave.Dispose();
    }

    private async UniTaskVoid SaveAsync()
    {
        var data = new PlayerData
        {
            Gold = _gold.Value,
            Gem = _gem.Value,
            BestWave = _bestWave.Value,
            HunterCardGachaPity = HunterCardGachaPity,
        };

        for (int i = 0; i < _hunters.Count; i++)
        {
            var hunter = _hunters[i];
            data.Hunters.Add(new HunterRecord
            {
                TowerId = hunter.TowerId,
                Level = hunter.Level.Value,
                Unlocked = hunter.Unlocked.Value,
                Cards = hunter.Cards.Value,
            });
        }

        await _store.SaveAsync(data);
    }
}
