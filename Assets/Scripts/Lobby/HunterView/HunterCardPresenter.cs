using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.U2D;
using VContainer.Unity;

public class HunterCardPresenter : IStartable, IDisposable
{
    private enum SortKey
    {
        Level,
        Grade,
    }

    private readonly struct SortState
    {
        public readonly SortKey Key;
        public readonly bool Ascending;
        public readonly string Label;

        public SortState(SortKey key, bool ascending, string label)
        {
            Key = key;
            Ascending = ascending;
            Label = label;
        }
    }

    private static readonly SortState[] SortCycle =
    {
        new SortState(SortKey.Level, true, "레벨순"),
        new SortState(SortKey.Level, false, "레벨순"),
        new SortState(SortKey.Grade, true, "등급순"),
        new SortState(SortKey.Grade, false, "등급순"),
    };

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
    private readonly IResourceLoadService _resourceLoadService;
    private readonly HunterCardView _view;
    private readonly List<Entry> _entries = new List<Entry>();
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private SpriteAtlasCache _thumbnails;
    private int _sortIndex;

    public HunterCardPresenter(
        IPlayerDataService playerDataService,
        IPopupService popupService,
        IResourceLoadService resourceLoadService,
        HunterCardView view)
    {
        _playerDataService = playerDataService;
        _popupService = popupService;
        _resourceLoadService = resourceLoadService;
        _view = view;
    }

    public void Start()
    {
        StartAsync().Forget();
    }

    private async UniTaskVoid StartAsync()
    {
        var cardObject = await _resourceLoadService.LoadAsync<GameObject>(PrefabKeys.HunterCard);
        var thumbnailAtlas = await _resourceLoadService.LoadAsync<SpriteAtlas>(AtlasKeys.ThumbnailAtlas);
        if (_disposables.IsDisposed)
            return;

        if (thumbnailAtlas == null)
            Debug.Log($"[HunterCardPresenter] {AtlasKeys.ThumbnailAtlas} 로드에 실패했습니다.");

        _thumbnails = new SpriteAtlasCache(thumbnailAtlas);

        if (cardObject == null || !cardObject.TryGetComponent<HunterCard>(out var cardPrefab))
        {
            Debug.Log($"[HunterCardPresenter] {PrefabKeys.HunterCard} 로드에 실패했습니다.");
            return;
        }

        foreach (var hunter in _playerDataService.Hunters)
        {
            if (!hunter.Data.Enabled)
                continue;

            var entry = new Entry(hunter, _view.CreateCard(cardPrefab));
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
            _thumbnails.GetSprite(hunter.Data.ThumbnailName),
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
        _sortIndex = (_sortIndex + 1) % SortCycle.Length;
        ApplySort();
    }

    // 레벨순은 레벨이 같으면 등급 오름차순, 마지막은 항상 타워 ID 순이다.
    private void ApplySort()
    {
        var state = SortCycle[_sortIndex];
        int direction = state.Ascending ? 1 : -1;
        _view.SetSort(state.Label, state.Ascending);

        _entries.Sort((a, b) =>
        {
            int result;

            if (state.Key == SortKey.Level)
            {
                result = direction * a.Hunter.Level.Value.CompareTo(b.Hunter.Level.Value);
                if (result != 0)
                    return result;

                result = a.Hunter.Data.GradeId.CompareTo(b.Hunter.Data.GradeId);
            }
            else
            {
                result = direction * a.Hunter.Data.GradeId.CompareTo(b.Hunter.Data.GradeId);
            }

            if (result != 0)
                return result;

            return a.Hunter.TowerId.CompareTo(b.Hunter.TowerId);
        });

        for (int i = 0; i < _entries.Count; i++)
            _entries[i].Card.transform.SetSiblingIndex(i);
    }
}