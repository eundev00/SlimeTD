using Cysharp.Threading.Tasks;
using SlimeTD.Protocol;
using System;
using UniRx;
using UnityEngine;
using UnityEngine.U2D;

public class HunterDetailPresenter : IPopupPresenter
{
    private readonly HunterDetailPopup _popup;
    private readonly IAccountService _accountService;
    private readonly IResourceLoadService _resourceLoadService;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private HunterState _hunter;
    private SkillCatalog _skillCatalog;
    private SpriteAtlas _skillAtlas;
    private SpriteAtlasCache _thumbnails = new SpriteAtlasCache(null);
    private bool _disposed;
    private bool _requesting;

    public HunterDetailPresenter(HunterDetailPopup popup, IAccountService accountService, IResourceLoadService resourceLoadService)
    {
        _popup = popup;
        _accountService = accountService;
        _resourceLoadService = resourceLoadService;
    }

    public void Initialize()
    {
        LoadSkillResourcesAsync().Forget();

        _popup.TowerId
            .Where(id => id > 0)
            .Subscribe(Bind)
            .AddTo(_disposables);

        _accountService.Coin.Subscribe(_ => Render()).AddTo(_disposables);
        _accountService.Diamond.Subscribe(_ => Render()).AddTo(_disposables);

        _popup.UpgradeClicked += HandleUpgradeClicked;
        _popup.BuyClicked += HandleBuyClicked;
        _popup.CloseClicked += HandleCloseClicked;
    }

    // 전역 ResourceLoadService가 캐시하므로 팝업을 다시 열어도 다시 읽지 않는다.
    private async UniTaskVoid LoadSkillResourcesAsync()
    {
        var catalog = await _resourceLoadService.LoadAsync<SkillCatalog>(DataKeys.SkillCatalog);
        var atlas = await _resourceLoadService.LoadAsync<SpriteAtlas>(AtlasKeys.SkillIconAtlas);
        var thumbnailAtlas = await _resourceLoadService.LoadAsync<SpriteAtlas>(AtlasKeys.ThumbnailAtlas);

        if (_disposed)
            return;

        if (catalog == null)
            Debug.Log($"[HunterDetailPresenter] {DataKeys.SkillCatalog} 로드에 실패했습니다.");

        if (atlas == null)
            Debug.Log($"[HunterDetailPresenter] {AtlasKeys.SkillIconAtlas} 로드에 실패했습니다.");

        if (thumbnailAtlas == null)
            Debug.Log($"[HunterDetailPresenter] {AtlasKeys.ThumbnailAtlas} 로드에 실패했습니다.");

        _thumbnails = new SpriteAtlasCache(thumbnailAtlas);
        _skillCatalog = catalog;
        _skillAtlas = atlas;
        Render();
    }

    public void Dispose()
    {
        _disposed = true;
        _popup.UpgradeClicked -= HandleUpgradeClicked;
        _popup.BuyClicked -= HandleBuyClicked;
        _popup.CloseClicked -= HandleCloseClicked;
        _disposables.Dispose();
    }

    private void Bind(int towerId)
    {
        if (!_accountService.TryGetHunter(towerId, out var hunter))
        {
            Debug.Log($"[HunterDetailPresenter] 헌터를 찾을 수 없습니다: {towerId}");
            return;
        }

        _hunter = hunter;

        // 헌터가 바뀔 때마다 이전 헌터의 구독을 걷어내고 새로 건다.
        _hunterDisposables?.Dispose();
        _hunterDisposables = new CompositeDisposable();
        hunter.Level.Subscribe(_ => Render()).AddTo(_hunterDisposables);
        hunter.Unlocked.Subscribe(_ => Render()).AddTo(_hunterDisposables);
        hunter.Cards.Subscribe(_ => Render()).AddTo(_hunterDisposables);
    }

    private CompositeDisposable _hunterDisposables;

    private void HandleUpgradeClicked()
    {
        if (_hunter == null || _requesting)
            return;

        _accountService.TryGetLevelUpCost(_hunter.TowerId, out _, out int neededCoin);
        if (IsCoinShort(neededCoin))
        {
            // TODO: 상점 팝업으로 이동
            Debug.Log("[HunterDetailPresenter] 코인 부족: 상점 이동 예정");
            _popup.ShowMessage("코인이 부족합니다");
            return;
        }

        RequestAsync(_accountService.LevelUpAsync(_hunter.TowerId)).Forget();
    }

    private void HandleBuyClicked()
    {
        if (_hunter == null || _requesting)
            return;

        RequestAsync(_accountService.PurchaseHunterAsync(_hunter.TowerId)).Forget();
    }

    // 응답을 기다리는 동안 버튼을 잠가 같은 요청이 두 번 가지 않게 한다
    private async UniTaskVoid RequestAsync(UniTask<ResultCode> request)
    {
        _requesting = true;
        _popup.ShowMessage(string.Empty);
        Render();

        var result = await request;
        if (_disposed)
            return;

        _requesting = false;
        _popup.ShowMessage(ResultMessage(result));
        Render();
    }

    private static string ResultMessage(ResultCode result)
    {
        return result switch
        {
            ResultCode.Ok => string.Empty,
            ResultCode.NotEnoughItems => "재화나 카드가 부족합니다",
            ResultCode.MaxLevel => "이미 최대 레벨입니다",
            ResultCode.HunterLocked => "보유하지 않은 헌터입니다",
            ResultCode.AlreadyUnlocked => "이미 보유한 헌터입니다",
            ResultCode.NotPurchasable => "구매할 수 없는 헌터입니다",
            _ => "서버 요청에 실패했습니다",
        };
    }

    private void HandleCloseClicked()
    {
        _popup.RequestClose();
    }

    private void Render()
    {
        if (_hunter == null)
            return;

        bool unlocked = _hunter.Unlocked.Value;
        int level = _hunter.Level.Value;
        bool maxLevel = level >= TowerLevelService.MaxLevel;
        bool canLevelUp = unlocked && !maxLevel && _accountService.CanLevelUp(_hunter.TowerId);

        RenderInfo();
        _popup.SetThumbnail(_thumbnails.GetSprite(_hunter.Data.ThumbnailName), unlocked);
        _popup.SetUnlocked(unlocked);

        if (unlocked)
            RenderLevel(level, maxLevel, canLevelUp);

        RenderStats(level, maxLevel);
        RenderAction(unlocked, maxLevel);

        // 보유하지 않은 헌터는 레벨 0으로 넘겨 모든 스킬을 잠김으로 보여 준다.
        _popup.SetSkills(_skillCatalog, _skillAtlas, _hunter.TowerId, unlocked ? level : 0);
    }

    private void RenderInfo()
    {
        var data = _hunter.Data;
        _popup.SetHunterInfo(data.TowerName, data.GradeId);
    }

    private void RenderLevel(int level, bool maxLevel, bool canLevelUp)
    {
        _popup.SetLevel(level, TowerLevelService.MaxLevel, canLevelUp);

        // 전설처럼 레벨업에 카드를 쓰지 않는 헌터는 카드 게이지를 숨긴다
        bool usesCards = _hunter.Data.LevelUpCost.GetCards(1) > 0;
        _popup.SetCardProgressVisible(usesCards);
        if (!usesCards)
            return;

        if (maxLevel)
        {
            _popup.SetCardProgressMax();
            return;
        }

        _accountService.TryGetLevelUpCost(_hunter.TowerId, out int neededCards, out _);
        _popup.SetCardProgress(_hunter.Cards.Value, neededCards);
    }

    // 레벨업 조건(카드/코인)이 안 돼도 다음 레벨 공격력 증가량은 보여 준다.
    private void RenderStats(int level, bool maxLevel)
    {
        var data = _hunter.Data;

        string attackBonus = string.Empty;
        if (!maxLevel)
        {
            int delta = Mathf.RoundToInt(data.GetAttack(level + 1)) - Mathf.RoundToInt(data.GetAttack(level));
            attackBonus = delta > 0 ? $"+{delta}" : string.Empty;
        }

        float attackSpeed = data.AttackCooldown > 0f ? data.AttackSpeedPercent / 100f / data.AttackCooldown : 0f;

        _popup.SetStats(
            Mathf.RoundToInt(data.GetAttack(level)).ToString("N0"),
            attackBonus,
            attackSpeed.ToString("0.0#"));
    }

    private void RenderAction(bool unlocked, bool maxLevel)
    {
        int towerId = _hunter.TowerId;

        if (!unlocked)
        {
            int price = _hunter.Data.UnlockDiamondPrice;
            if (price <= 0)
            {
                _popup.ShowUnavailable();
                return;
            }

            _popup.ShowBuy(price.ToString("N0"), !_requesting && _accountService.Diamond.Value >= price);
            return;
        }

        if (maxLevel)
        {
            _popup.ShowMaxLevel();
            return;
        }

        // 코인만 모자라면 버튼은 눌리게 두고 가격만 빨갛게 한다 (누르면 상점으로 보낼 예정)
        _accountService.TryGetLevelUpCost(towerId, out int neededCards, out int neededCoin);
        bool enoughCards = _hunter.Cards.Value >= neededCards;
        _popup.ShowUpgrade(neededCoin.ToString("N0"), !_requesting && enoughCards, IsCoinShort(neededCoin));
    }

    private bool IsCoinShort(int neededCoin)
    {
        return _accountService.Coin.Value < neededCoin;
    }
}
