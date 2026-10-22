using System;
using MessagePipe;
using UniRx;
using UnityEngine;

public class CurrencyViewPresenter : IDisposable
{
    private readonly CurrencyView _view;
    private readonly IPublisher<CurrencyViewClickedEvent> _clickedPublisher;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    public CurrencyViewPresenter(CurrencyView view, IPlayerDataService playerDataService, IPublisher<CurrencyViewClickedEvent> clickedPublisher)
    {
        _view = view;
        _clickedPublisher = clickedPublisher;

        GetSource(view.Type, playerDataService)
            .Subscribe(view.SetValue)
            .AddTo(_disposables);

        view.Clicked += HandleClicked;
    }

    public void Dispose()
    {
        _view.Clicked -= HandleClicked;
        _disposables.Dispose();
    }

    private void HandleClicked()
    {
        Debug.Log($"[CurrencyViewPresenter] 클릭: {_view.Type}");
        _clickedPublisher.Publish(new CurrencyViewClickedEvent(_view.Type));
        // TODO: 상점이 생기면 CurrencyViewClickedEvent를 받아 상점 열기
    }

    private static IReadOnlyReactiveProperty<int> GetSource(CurrencyType type, IPlayerDataService service)
    {
        switch (type)
        {
            case CurrencyType.Gold: return service.Gold;
            case CurrencyType.Gem: return service.Gem;
            default: return service.Cards;
        }
    }
}
