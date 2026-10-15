using System;
using UniRx;
using VContainer.Unity;

public class LobbyHeaderPresenter : IStartable, IDisposable
{
    private readonly IPlayerDataService _playerDataService;
    private readonly LobbyHeader _view;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    public LobbyHeaderPresenter(IPlayerDataService playerDataService, LobbyHeader view)
    {
        _playerDataService = playerDataService;
        _view = view;
    }

    public void Start()
    {
        _playerDataService.Gold
            .Subscribe(_view.SetGold)
            .AddTo(_disposables);

        _playerDataService.Gem
            .Subscribe(_view.SetGem)
            .AddTo(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}