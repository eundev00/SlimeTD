using System;
using MessagePipe;
using UniRx;
using UnityEngine;

public class CurrencyViewPresenter : IDisposable
{
    private readonly CurrencyView _view;
    private readonly IPublisher<CurrencyViewClickedEvent> _clickedPublisher;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    public CurrencyViewPresenter(CurrencyView view, IAccountService accountService, IPublisher<CurrencyViewClickedEvent> clickedPublisher)
    {
        _view = view;
        _clickedPublisher = clickedPublisher;

        if (view.Type == CurrencyType.Energy)
        {
            accountService.Energy
                .CombineLatest(accountService.EnergyMax, (energy, max) => $"{energy}/{max}")
                .Subscribe(view.SetText)
                .AddTo(_disposables);
        }
        else
        {
            GetSource(view.Type, accountService)
                .Subscribe(view.SetValue)
                .AddTo(_disposables);
        }

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

    private static IReadOnlyReactiveProperty<int> GetSource(CurrencyType type, IAccountService service)
    {
        switch (type)
        {
            case CurrencyType.Diamond: return service.Diamond;
            case CurrencyType.PackTicket: return service.PackTicket;
            default: return service.Coin;
        }
    }
}
