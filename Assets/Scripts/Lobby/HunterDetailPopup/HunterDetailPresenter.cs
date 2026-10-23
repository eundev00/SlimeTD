using System;
using UniRx;
using UnityEngine;

public class HunterDetailPresenter : IPopupPresenter
{
    private readonly HunterDetailPopup _popup;
    private readonly IPlayerDataService _playerDataService;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private HunterState _hunter;

    public HunterDetailPresenter(HunterDetailPopup popup, IPlayerDataService playerDataService)
    {
        _popup = popup;
        _playerDataService = playerDataService;
    }

    public void Initialize()
    {
        _popup.TowerId
            .Where(id => id > 0)
            .Subscribe(Bind)
            .AddTo(_disposables);

        _playerDataService.Gold.Subscribe(_ => Render()).AddTo(_disposables);
        _playerDataService.Gem.Subscribe(_ => Render()).AddTo(_disposables);
        _playerDataService.Cards.Subscribe(_ => Render()).AddTo(_disposables);

        _popup.UpgradeClicked += HandleUpgradeClicked;
        _popup.BuyClicked += HandleBuyClicked;
        _popup.CloseClicked += HandleCloseClicked;
    }

    public void Dispose()
    {
        _popup.UpgradeClicked -= HandleUpgradeClicked;
        _popup.BuyClicked -= HandleBuyClicked;
        _popup.CloseClicked -= HandleCloseClicked;
        _disposables.Dispose();
    }

    private void Bind(int towerId)
    {
        if (!_playerDataService.TryGetHunter(towerId, out var hunter))
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
    }

    private CompositeDisposable _hunterDisposables;

    private void HandleUpgradeClicked()
    {
        if (_hunter == null)
            return;

        _playerDataService.TryLevelUp(_hunter.TowerId);
    }

    private void HandleBuyClicked()
    {
        if (_hunter == null)
            return;

        _playerDataService.TryPurchaseHunter(_hunter.TowerId);
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
        bool canLevelUp = unlocked && !maxLevel && _playerDataService.CanLevelUp(_hunter.TowerId);

        RenderInfo();
        _popup.SetThumbnail(_hunter.Data.ThumbnailLarge, unlocked);
        _popup.SetUnlocked(unlocked);

        if (unlocked)
            RenderLevel(level, maxLevel, canLevelUp);

        RenderStats(level, canLevelUp);
        RenderAction(unlocked, maxLevel);
    }

    private void RenderInfo()
    {
        var data = _hunter.Data;
        _popup.SetHunterInfo(data.TowerName, data.GradeId, data.GradeName);
    }

    private void RenderLevel(int level, bool maxLevel, bool canLevelUp)
    {
        _popup.SetLevel(level, TowerLevelService.MaxLevel, canLevelUp);

        if (maxLevel)
        {
            _popup.SetCardProgressMax();
            return;
        }

        _playerDataService.TryGetLevelUpCost(_hunter.TowerId, out int neededCards, out _);
        _popup.SetCardProgress(_playerDataService.Cards.Value, neededCards);
    }

    private void RenderStats(int level, bool canLevelUp)
    {
        var data = _hunter.Data;

        string attackBonus = string.Empty;
        if (canLevelUp)
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
            int price = _hunter.Data.UnlockGemPrice;
            if (price <= 0)
            {
                _popup.ShowUnavailable();
                return;
            }

            _popup.ShowBuy(price.ToString("N0"), _playerDataService.Gem.Value >= price);
            return;
        }

        if (maxLevel)
        {
            _popup.ShowMaxLevel();
            return;
        }

        _playerDataService.TryGetLevelUpCost(towerId, out _, out int neededGold);
        _popup.ShowUpgrade(neededGold.ToString("N0"), _playerDataService.CanLevelUp(towerId));
    }
}
