using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.U2D;

public class HunterCardRevealPresenter : IPopupPresenter
{
    private const string OpenAllText = "모두 열기";
    private const string OpenOneText = "열기";
    private const string ConfirmText = "확인";
    private const int OpenAllIntervalMs = 90;

    private readonly HunterCardRevealPopup _popup;
    private readonly IPlayerDataService _playerDataService;
    private readonly IResourceLoadService _resourceLoadService;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();

    private bool[] _opened = Array.Empty<bool>();
    private bool _openingAll;
    private bool _disposed;

    public HunterCardRevealPresenter(HunterCardRevealPopup popup, IPlayerDataService playerDataService, IResourceLoadService resourceLoadService)
    {
        _popup = popup;
        _playerDataService = playerDataService;
        _resourceLoadService = resourceLoadService;
    }

    public void Initialize()
    {
        _popup.CardClicked += HandleCardClicked;
        _popup.ActionClicked += HandleActionClicked;

        _popup.Result
            .Where(result => result != null)
            .Subscribe(result => BindAsync(result).Forget())
            .AddTo(_disposables);
    }

    public void Dispose()
    {
        _disposed = true;
        _popup.CardClicked -= HandleCardClicked;
        _popup.ActionClicked -= HandleActionClicked;
        _disposables.Dispose();
        _cts.Cancel();
        _cts.Dispose();
    }

    private async UniTaskVoid BindAsync(HunterCardGachaResult result)
    {
        // 썸네일을 읽는 동안 눌리지 않도록 결과가 준비될 때까지 비워 둔다
        _opened = Array.Empty<bool>();

        var atlas = await _resourceLoadService.LoadAsync<SpriteAtlas>(AtlasKeys.ThumbnailAtlas);
        if (_disposed)
            return;

        if (atlas == null)
            Debug.Log($"[HunterCardRevealPresenter] {AtlasKeys.ThumbnailAtlas} 로드에 실패했습니다.");

        var thumbnails = new SpriteAtlasCache(atlas);
        var items = new List<HunterCardRevealItem>(result.Cards.Count);
        foreach (var card in result.Cards)
        {
            if (_playerDataService.TryGetHunter(card.TowerId, out var hunter))
                items.Add(new HunterCardRevealItem(hunter.Data.TowerName, card.GradeId, thumbnails.GetSprite(hunter.Data.ThumbnailName)));
            else
                items.Add(new HunterCardRevealItem(card.TowerId.ToString(), card.GradeId, null));
        }

        _opened = new bool[items.Count];
        _openingAll = false;
        _popup.ShowCards(items);
        RenderAction();
    }

    private void HandleCardClicked(int index)
    {
        Open(index);
    }

    private void HandleActionClicked()
    {
        if (_opened.Length == 0)
            return;

        if (AllOpened())
        {
            _popup.RequestClose();
            return;
        }

        if (!_openingAll)
            OpenAllAsync().Forget();
    }

    private async UniTaskVoid OpenAllAsync()
    {
        _openingAll = true;
        var opened = _opened;

        for (int i = 0; i < opened.Length; i++)
        {
            if (opened[i])
                continue;

            Open(i);

            bool canceled = await UniTask.Delay(OpenAllIntervalMs, cancellationToken: _cts.Token).SuppressCancellationThrow();
            // 여는 도중 새 결과로 바뀌면 이전 결과를 계속 열지 않는다
            if (canceled || _opened != opened)
                return;
        }

        _openingAll = false;
    }

    private void Open(int index)
    {
        if (index < 0 || index >= _opened.Length || _opened[index])
            return;

        _opened[index] = true;
        _popup.Flip(index);
        RenderAction();
    }

    private void RenderAction()
    {
        if (AllOpened())
            _popup.SetActionText(ConfirmText);
        else
            _popup.SetActionText(_opened.Length == 1 ? OpenOneText : OpenAllText);
    }

    private bool AllOpened()
    {
        foreach (bool opened in _opened)
        {
            if (!opened)
                return false;
        }

        return true;
    }
}
