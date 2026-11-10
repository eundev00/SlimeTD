using System;
using UniRx;
using VContainer.Unity;

public class WaveScrollPresenter : IStartable, IDisposable
{
    private readonly IPlayerDataService _playerDataService;
    private readonly StageData _stage;
    private readonly WaveScrollView _view;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    public WaveScrollPresenter(IPlayerDataService playerDataService, StageData stage, WaveScrollView view)
    {
        _playerDataService = playerDataService;
        _stage = stage;
        _view = view;
    }

    public void Start()
    {
        _playerDataService.BestWave
            .Subscribe(wave => _view.SetBestWave(wave, _stage.FinalWave))
            .AddTo(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
