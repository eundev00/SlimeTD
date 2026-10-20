using System;
using UniRx;
using VContainer.Unity;

public class BattleMapPresenter : IStartable, IDisposable
{
    private readonly IPlayerDataService _playerDataService;
    private readonly BattleMap _view;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    public BattleMapPresenter(IPlayerDataService playerDataService, BattleMap view)
    {
        _playerDataService = playerDataService;
        _view = view;
    }

    public void Start()
    {
        _playerDataService.BestWave
            .Subscribe(_view.SetBestWave)
            .AddTo(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
