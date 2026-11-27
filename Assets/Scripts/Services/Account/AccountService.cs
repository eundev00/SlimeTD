using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Google.Protobuf.Collections;
using SlimeTD.Protocol;
using UniRx;
using UnityEngine;

public class AccountService : IAccountService, IDisposable
{
    private readonly IResourceLoadService _resourceLoadService;
    private readonly ITowerLevelService _towerLevelService;
    private readonly IGameServerConnection _connection;

    private readonly ReactiveProperty<int> _coin = new ReactiveProperty<int>();
    private readonly ReactiveProperty<int> _diamond = new ReactiveProperty<int>();
    private readonly ReactiveProperty<int> _packTicket = new ReactiveProperty<int>();
    private readonly ReactiveProperty<int> _energy = new ReactiveProperty<int>();
    private readonly ReactiveProperty<int> _energyMax = new ReactiveProperty<int>();
    private readonly ReactiveProperty<long> _energyRecoverAtMs = new ReactiveProperty<long>();
    private readonly ReactiveProperty<int> _bestWave = new ReactiveProperty<int>();
    private readonly List<HunterState> _hunters = new List<HunterState>();
    private readonly Dictionary<int, HunterState> _huntersById = new Dictionary<int, HunterState>();

    private int _coinItemId;
    private int _diamondItemId;
    private int _packTicketItemId;
    private int _energyItemId;

    public bool IsInitialized { get; private set; }
    public IReadOnlyReactiveProperty<int> Coin => _coin;
    public IReadOnlyReactiveProperty<int> Diamond => _diamond;
    public IReadOnlyReactiveProperty<int> PackTicket => _packTicket;
    public IReadOnlyReactiveProperty<int> Energy => _energy;
    public IReadOnlyReactiveProperty<int> EnergyMax => _energyMax;
    public IReadOnlyReactiveProperty<long> EnergyRecoverAtMs => _energyRecoverAtMs;
    public long ServerNowMs => _connection.ServerNowMs;
    public IReadOnlyReactiveProperty<int> BestWave => _bestWave;
    public IReadOnlyList<HunterState> Hunters => _hunters;

    public AccountService(
        IResourceLoadService resourceLoadService,
        ITowerLevelService towerLevelService,
        IGameServerConnection connection)
    {
        _resourceLoadService = resourceLoadService;
        _towerLevelService = towerLevelService;
        _connection = connection;
    }

    public async UniTask<bool> InitializeAsync()
    {
        if (IsInitialized)
            return true;

        var catalog = await _resourceLoadService.LoadAsync<TowerCatalog>(DataKeys.TowerCatalog);
        var items = await _resourceLoadService.LoadAsync<ItemCatalog>(DataKeys.ItemCatalog);
        var bestWaveStage = await _resourceLoadService.LoadAsync<StageData>(DataKeys.Stage_SingleForest01);
        if (catalog == null || items == null || bestWaveStage == null)
        {
            Debug.Log("[AccountService] 타워·아이템·스테이지 데이터를 읽지 못했습니다.");
            return false;
        }

        if (!items.TryGetItemId(ItemCatalog.Coin, out _coinItemId) || !items.TryGetItemId(ItemCatalog.Diamond, out _diamondItemId)
            || !items.TryGetItemId(ItemCatalog.PackTicket, out _packTicketItemId) || !items.TryGetItemId(ItemCatalog.Energy, out _energyItemId))
        {
            Debug.Log("[AccountService] ItemCatalog에 Coin, Diamond, PackTicket, Energy 중 없는 것이 있습니다.");
            return false;
        }

        if (!await _connection.ConnectAsync())
            return false;

        try
        {
            var login = await _connection.RequestAsync(MsgId.LoginReq, new LoginReq { DeviceId = DeviceId.Get() }, MsgId.LoginRes, LoginRes.Parser);
            if (login.Result != ResultCode.Ok)
            {
                Debug.Log($"[AccountService] 로그인 실패: {login.Result}");
                return false;
            }

            var info = await _connection.RequestAsync(MsgId.GetPlayerInfoReq, new GetPlayerInfoReq(), MsgId.GetPlayerInfoRes, GetPlayerInfoRes.Parser);
            if (info.Result != ResultCode.Ok)
            {
                Debug.Log($"[AccountService] 계정 정보 받기 실패: {info.Result}");
                return false;
            }

            Debug.Log($"[AccountService] 로그인: 계정 {login.AccountId}, 새 계정 {login.IsNewAccount}");
            Apply(info.Player, catalog, bestWaveStage.StageId);
        }
        catch (Exception e)
        {
            Debug.Log($"[AccountService] 서버 요청 실패: {e.Message}");
            return false;
        }

        IsInitialized = true;
        return true;
    }

    private void Apply(PlayerInfo player, TowerCatalog catalog, int bestWaveStageId)
    {
        ClearHunters();

        var serverHunters = new Dictionary<int, Hunter>();
        foreach (var hunter in player.Hunters)
            serverHunters[hunter.TowerId] = hunter;

        foreach (var tower in catalog.Towers)
        {
            if (tower == null || _huntersById.ContainsKey(tower.Id))
                continue;

            if (!serverHunters.TryGetValue(tower.Id, out var record))
            {
                Debug.Log($"[AccountService] 서버 헌터 목록에 없는 타워입니다 (데이터 버전 차이): {tower.Id} {tower.CodeName}");
                continue;
            }

            var state = new HunterState(tower, record.Level, record.Unlocked, record.Cards);
            _hunters.Add(state);
            _huntersById.Add(tower.Id, state);
            _towerLevelService.SetLevel(tower.Id, record.Level);
        }

        ApplyItems(player.Items);
        _energyMax.Value = player.EnergyMax;
        SetEnergyRecoverAt(player.EnergyRecoverAtMs);

        int bestWave = 0;
        foreach (var stage in player.Stages)
        {
            if (stage.StageId == bestWaveStageId)
                bestWave = stage.BestWave;
        }

        _bestWave.Value = bestWave;
    }

    // 서버 응답의 아이템 현재 수량으로 계정 재화를 맞춘다
    public void ApplyItems(IEnumerable<ItemStack> items)
    {
        foreach (var item in items)
        {
            int amount = (int)Math.Min(item.Amount, int.MaxValue);
            if (item.ItemId == _coinItemId)
                _coin.Value = amount;
            else if (item.ItemId == _diamondItemId)
                _diamond.Value = amount;
            else if (item.ItemId == _packTicketItemId)
                _packTicket.Value = amount;
            else if (item.ItemId == _energyItemId)
                _energy.Value = amount;
        }
    }

    // 다음 에너지 1 회복 시각 (서버 시계 Unix ms). 0이면 최대치 이상이라 회복하지 않는다
    public void SetEnergyRecoverAt(long serverMs)
    {
        _energyRecoverAtMs.Value = serverMs;
    }

    // 계정 재화와 에너지 회복 시각만 서버 값으로 다시 맞춘다 (에너지 회복 시각이 지났을 때 등)
    public async UniTask RefreshItemsAsync()
    {
        if (!IsInitialized)
            return;

        try
        {
            var info = await _connection.RequestAsync(MsgId.GetPlayerInfoReq, new GetPlayerInfoReq(), MsgId.GetPlayerInfoRes, GetPlayerInfoRes.Parser);
            if (info.Result != ResultCode.Ok)
            {
                Debug.Log($"[AccountService] 계정 정보 갱신 실패: {info.Result}");
                return;
            }

            ApplyItems(info.Player.Items);
            _energyMax.Value = info.Player.EnergyMax;
            SetEnergyRecoverAt(info.Player.EnergyRecoverAtMs);
        }
        catch (Exception e)
        {
            Debug.Log($"[AccountService] 계정 정보 갱신 요청 실패: {e.Message}");
        }
    }

    private void ApplyHunter(Hunter record)
    {
        if (record == null || !_huntersById.TryGetValue(record.TowerId, out var hunter))
            return;

        hunter.Cards.Value = record.Cards;
        hunter.Unlocked.Value = record.Unlocked;
        hunter.Level.Value = record.Level;
        _towerLevelService.SetLevel(record.TowerId, record.Level);
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

    public async UniTask<ResultCode> PurchaseHunterAsync(int towerId)
    {
        if (!IsInitialized)
            return ResultCode.NotLoggedIn;

        try
        {
            var res = await _connection.RequestAsync(MsgId.HunterPurchaseReq, new HunterPurchaseReq { TowerId = towerId }, MsgId.HunterPurchaseRes, HunterPurchaseRes.Parser);
            if (res.Result == ResultCode.Ok)
            {
                ApplyItems(res.Items);
                ApplyHunter(res.Hunter);
            }

            return res.Result;
        }
        catch (Exception e)
        {
            Debug.Log($"[AccountService] 헌터 구매 요청 실패: {e.Message}");
            return ResultCode.ServerError;
        }
    }

    public async UniTask<ResultCode> LevelUpAsync(int towerId)
    {
        if (!IsInitialized)
            return ResultCode.NotLoggedIn;

        try
        {
            var res = await _connection.RequestAsync(MsgId.HunterLevelUpReq, new HunterLevelUpReq { TowerId = towerId }, MsgId.HunterLevelUpRes, HunterLevelUpRes.Parser);
            if (res.Result == ResultCode.Ok)
            {
                ApplyItems(res.Items);
                ApplyHunter(res.Hunter);
            }

            return res.Result;
        }
        catch (Exception e)
        {
            Debug.Log($"[AccountService] 헌터 레벨업 요청 실패: {e.Message}");
            return ResultCode.ServerError;
        }
    }

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

    // 버튼 표시용이다. 최종 판단은 서버가 한다
    public bool CanLevelUp(int towerId)
    {
        if (!TryGetLevelUpCost(towerId, out int cards, out int coin))
            return false;

        var hunter = _huntersById[towerId];
        return hunter.Unlocked.Value && hunter.Cards.Value >= cards && _coin.Value >= coin;
    }

    // TODO: 아래는 서버 기능이 아직 없어 이 화면에만 반영되고 저장되지 않는다
    public void UnlockHunter(int towerId)
    {
        if (!_huntersById.TryGetValue(towerId, out var hunter) || hunter.Unlocked.Value)
            return;

        WarnNotSaved(nameof(UnlockHunter));
        hunter.Unlocked.Value = true;
    }

    public void AddCoin(int amount)
    {
        if (amount == 0)
            return;

        WarnNotSaved(nameof(AddCoin));
        _coin.Value = Mathf.Max(0, _coin.Value + amount);
    }

    public void AddDiamond(int amount)
    {
        if (amount == 0)
            return;

        WarnNotSaved(nameof(AddDiamond));
        _diamond.Value = Mathf.Max(0, _diamond.Value + amount);
    }

    public void AddHunterCards(int towerId, int amount)
    {
        if (amount == 0 || !_huntersById.TryGetValue(towerId, out var hunter))
            return;

        WarnNotSaved(nameof(AddHunterCards));
        hunter.Cards.Value = Mathf.Max(0, hunter.Cards.Value + amount);
    }

    // 서버가 싱글 전투 결과를 확인하고 기록한 값을 받는다
    public void SetBestWave(int wave)
    {
        _bestWave.Value = wave;
    }

    // 확률은 서버가 굴린다. 카드 목록은 연출용이고, 계정 값은 응답의 아이템·헌터로 갱신한다
    public async UniTask<(ResultCode Result, IReadOnlyList<HunterCardGachaCard> Cards)> DrawHunterCardGachaAsync(int packId)
    {
        if (!IsInitialized)
            return (ResultCode.NotLoggedIn, null);

        try
        {
            var res = await _connection.RequestAsync(MsgId.HunterCardGachaDrawReq, new HunterCardGachaDrawReq { PackId = packId }, MsgId.HunterCardGachaDrawRes, HunterCardGachaDrawRes.Parser);
            if (res.Result != ResultCode.Ok)
                return (res.Result, null);

            ApplyItems(res.Items);
            foreach (var hunter in res.Hunters)
                ApplyHunter(hunter);

            var cards = new List<HunterCardGachaCard>(res.Cards.Count);
            foreach (var card in res.Cards)
            {
                var kind = card.Kind == GachaCardKind.Refund ? HunterCardGachaCardKind.Refund : HunterCardGachaCardKind.Card;
                cards.Add(new HunterCardGachaCard(card.TowerId, card.GradeId, kind, (int)card.RefundCoin));
            }

            return (ResultCode.Ok, cards);
        }
        catch (Exception e)
        {
            Debug.Log($"[AccountService] 헌터 카드 뽑기 요청 실패: {e.Message}");
            return (ResultCode.ServerError, null);
        }
    }

    private static void WarnNotSaved(string action)
    {
        Debug.LogWarning($"[AccountService] 서버 미반영: {action}은 서버 기능이 없어 저장되지 않습니다.");
    }

    private void ClearHunters()
    {
        foreach (var hunter in _hunters)
            hunter.Dispose();

        _hunters.Clear();
        _huntersById.Clear();
    }

    public void Dispose()
    {
        ClearHunters();
        _coin.Dispose();
        _diamond.Dispose();
        _packTicket.Dispose();
        _energy.Dispose();
        _energyMax.Dispose();
        _energyRecoverAtMs.Dispose();
        _bestWave.Dispose();
    }
}