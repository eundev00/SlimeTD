using System;
using UniRx;
using UnityEngine;

public class HunterDetailPresenter : IPopupPresenter
{
    private static readonly string[] GradeNames = { "일반", "희귀", "영웅", "전설" };

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

        _popup.ActionClicked += HandleActionClicked;
        _popup.CloseClicked += HandleCloseClicked;
    }

    public void Dispose()
    {
        _popup.ActionClicked -= HandleActionClicked;
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

    private void HandleActionClicked()
    {
        if (_hunter == null)
            return;

        if (_hunter.Unlocked.Value)
            _playerDataService.TryLevelUp(_hunter.TowerId);
        else
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

        _popup.Render(BuildViewData(_hunter));
    }

    private HunterDetailViewData BuildViewData(HunterState hunter)
    {
        var data = hunter.Data;
        int towerId = hunter.TowerId;
        int level = hunter.Level.Value;
        bool unlocked = hunter.Unlocked.Value;
        bool maxLevel = level >= TowerLevelService.MaxLevel;

        int cards = _playerDataService.Cards.Value;
        int gold = _playerDataService.Gold.Value;
        int gem = _playerDataService.Gem.Value;

        _playerDataService.TryGetLevelUpCost(towerId, out int neededCards, out int neededGold);

        string attackBonus = string.Empty;
        if (!maxLevel)
        {
            int delta = Mathf.RoundToInt(data.GetAttack(level + 1)) - Mathf.RoundToInt(data.GetAttack(level));
            attackBonus = delta > 0 ? $"+{delta}" : string.Empty;
        }

        float attackSpeed = data.AttackCooldown > 0f ? data.AttackSpeedPercent / 100f / data.AttackCooldown : 0f;

        HunterDetailActionMode mode;
        bool actionEnabled;
        string costText;
        bool costInsufficient;

        if (!unlocked)
        {
            int price = data.UnlockGemPrice;
            mode = price > 0 ? HunterDetailActionMode.Unlock : HunterDetailActionMode.UnlockUnavailable;
            actionEnabled = price > 0 && gem >= price;
            costText = price.ToString("N0");
            costInsufficient = price > 0 && gem < price;
        }
        else if (maxLevel)
        {
            mode = HunterDetailActionMode.MaxLevel;
            actionEnabled = false;
            costText = string.Empty;
            costInsufficient = false;
        }
        else
        {
            mode = HunterDetailActionMode.LevelUp;
            actionEnabled = _playerDataService.CanLevelUp(towerId);
            costText = neededGold.ToString("N0");
            costInsufficient = gold < neededGold;
        }

        int gradeIndex = Mathf.Clamp(data.GradeId - 1, 0, GradeNames.Length - 1);

        return new HunterDetailViewData(
            data.TowerName,
            data.GradeId,
            GradeNames[gradeIndex],
            data.AttackType == TowerAttackType.Melee ? "근접" : "원거리",
            unlocked,
            level,
            TowerLevelService.MaxLevel,
            cards,
            neededCards,
            unlocked && !maxLevel && _playerDataService.CanLevelUp(towerId),
            Mathf.RoundToInt(data.GetAttack(level)).ToString("N0"),
            attackBonus,
            attackSpeed.ToString("0.0#"),
            mode,
            actionEnabled,
            costText,
            costInsufficient);
    }
}