using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniRx;
using VContainer.Unity;

public class HunterTabPresenter : IStartable, IDisposable
{
    private enum SortMode
    {
        Grade,
        Level,
    }

    private sealed class Entry
    {
        public readonly HunterState Hunter;
        public readonly HunterCard Card;

        public Entry(HunterState hunter, HunterCard card)
        {
            Hunter = hunter;
            Card = card;
        }
    }

    private readonly IPlayerDataService _playerDataService;
    private readonly IPopupService _popupService;
    private readonly HunterTab _view;
    private readonly List<Entry> _entries = new List<Entry>();
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private SortMode _sortMode = SortMode.Grade;

    public HunterTabPresenter(IPlayerDataService playerDataService, IPopupService popupService, HunterTab view)
    {
        _playerDataService = playerDataService;
        _popupService = popupService;
        _view = view;
    }

    public void Start()
    {
        foreach (var hunter in _playerDataService.Hunters)
        {
            if (!hunter.Data.Enabled)
                continue;

            var entry = new Entry(hunter, _view.CreateCard());
            _entries.Add(entry);

            int towerId = hunter.TowerId;
            entry.Card.Button.OnClickAsObservable()
                .Subscribe(_ => OpenDetailAsync(towerId).Forget())
                .AddTo(_disposables);

            hunter.Unlocked
                .Subscribe(_ => Refresh(entry))
                .AddTo(_disposables);

            hunter.Level
                .Subscribe(_ =>
                {
                    Refresh(entry);
                    ApplySort();
                })
                .AddTo(_disposables);
        }

        _playerDataService.Cards
            .Subscribe(cards =>
            {
                _view.SetCards(cards);
                RefreshAll();
            })
            .AddTo(_disposables);

        _playerDataService.Gold
            .Subscribe(_ => RefreshAll())
            .AddTo(_disposables);

        _view.SortClicked
            .Subscribe(_ => ToggleSort())
            .AddTo(_disposables);

        _view.SetSortLabel(GetSortLabel());
        ApplySort();
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    private async UniTaskVoid OpenDetailAsync(int towerId)
    {
        if (_popupService.HasOpenPopup)
            return;

        var popup = await _popupService.OpenAsync<HunterDetailPopup>(PrefabKeys.HunterDetailPopup);
        if (popup != null)
            popup.SetTowerId(towerId);
    }

    private void RefreshAll()
    {
        for (int i = 0; i < _entries.Count; i++)
            Refresh(_entries[i]);
    }

    private void Refresh(Entry entry)
    {
        var hunter = entry.Hunter;
        int level = hunter.Level.Value;
        bool maxLevel = level >= TowerLevelService.MaxLevel;

        _playerDataService.TryGetLevelUpCost(hunter.TowerId, out int neededCards, out _);

        entry.Card.Set(
            hunter.Data.TowerName,
            hunter.Data.GradeId,
            hunter.Unlocked.Value,
            level,
            maxLevel,
            _playerDataService.Cards.Value,
            neededCards,
            _playerDataService.CanLevelUp(hunter.TowerId));
    }

    private void ToggleSort()
    {
        _sortMode = _sortMode == SortMode.Grade ? SortMode.Level : SortMode.Grade;
        _view.SetSortLabel(GetSortLabel());
        ApplySort();
    }

    private string GetSortLabel()
    {
        return _sortMode == SortMode.Grade ? "등급순" : "레벨순";
    }

    // 등급순은 낮은 등급부터, 레벨순은 높은 레벨부터 보여 준다. 같으면 타워 ID 순이다.
    private void ApplySort()
    {
        _entries.Sort((a, b) =>
        {
            int result;

            if (_sortMode == SortMode.Level)
            {
                result = b.Hunter.Level.Value.CompareTo(a.Hunter.Level.Value);
                if (result != 0)
                    return result;
            }

            result = a.Hunter.Data.GradeId.CompareTo(b.Hunter.Data.GradeId);
            if (result != 0)
                return result;

            return a.Hunter.TowerId.CompareTo(b.Hunter.TowerId);
        });

        for (int i = 0; i < _entries.Count; i++)
            _entries[i].Card.transform.SetSiblingIndex(i);
    }
}