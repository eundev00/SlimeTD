using System;
using VContainer.Unity;

public class LobbyHeaderPresenter : IStartable, IDisposable
{
    private readonly IPlayerDataService _playerDataService;
    private readonly LobbyHeader _view;
    private CurrencyChipPresenter _goldPresenter;
    private CurrencyChipPresenter _gemPresenter;

    public LobbyHeaderPresenter(IPlayerDataService playerDataService, LobbyHeader view)
    {
        _playerDataService = playerDataService;
        _view = view;
    }

    public void Start()
    {
        _goldPresenter = new CurrencyChipPresenter(_view.GoldChip, _playerDataService);
        _gemPresenter = new CurrencyChipPresenter(_view.GemChip, _playerDataService);
    }

    public void Dispose()
    {
        _goldPresenter?.Dispose();
        _gemPresenter?.Dispose();
    }
}
