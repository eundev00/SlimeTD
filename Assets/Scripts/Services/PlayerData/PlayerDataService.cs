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
    private readonly ReactiveProperty<int> _cards = new ReactiveProperty<int>();
    private readonly List<HunterState> _hunters = new List<HunterState>();
    private readonly Dictionary<int, HunterState> _huntersById = new Dictionary<int, HunterState>();

    public bool IsInitialized { get; private set; }
    public IReadOnlyReactiveProperty<int> Gold => _gold;
    public IReadOnlyReactiveProperty<int> Gem => _gem;
    public IReadOnlyReactiveProperty<int> Cards => _cards;
    public IReadOnlyList<HunterState> Hunters => _hunters;

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

        var data = await _store.LoadAsync();
        bool isNew = data == null;
        if (isNew)
            data = new PlayerData();

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
        _cards.Value = data.Cards;
        IsInitialized = true;

        if (isNew || created)
            SaveAsync().Forget();
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

    public void AddGold(int amount)
    {
        if (amount == 0)
            return;

        _gold.Value = Mathf.Max(0, _gold.Value + amount);
        SaveAsync().Forget();
    }

    // 헌터 카드는 하나의 공용 재화다. 어느 헌터를 올릴지는 유저가 고른다.
    public void AddCards(int amount)
    {
        if (amount == 0)
            return;

        _cards.Value = Mathf.Max(0, _cards.Value + amount);
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
        return hunter.Unlocked.Value && _cards.Value >= cards && _gold.Value >= gold;
    }

    public bool TryLevelUp(int towerId)
    {
        if (!CanLevelUp(towerId))
            return false;

        TryGetLevelUpCost(towerId, out int cards, out int gold);

        var hunter = _huntersById[towerId];
        _cards.Value -= cards;
        _gold.Value -= gold;
        hunter.Level.Value += 1;

        _towerLevelService.SetLevel(towerId, hunter.Level.Value);
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
        _cards.Dispose();
    }

    private async UniTaskVoid SaveAsync()
    {
        var data = new PlayerData
        {
            Gold = _gold.Value,
            Gem = _gem.Value,
            Cards = _cards.Value,
        };

        for (int i = 0; i < _hunters.Count; i++)
        {
            var hunter = _hunters[i];
            data.Hunters.Add(new HunterRecord
            {
                TowerId = hunter.TowerId,
                Level = hunter.Level.Value,
                Unlocked = hunter.Unlocked.Value,
            });
        }

        await _store.SaveAsync(data);
    }
}
