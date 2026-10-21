using System;
using UniRx;
using VContainer.Unity;

public class WaveScrollPresenter : IStartable, IDisposable
{
    private readonly IPlayerDataService _playerDataService;
    private readonly WaveTableData _waveTable;
    private readonly WaveScrollView _view;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    public WaveScrollPresenter(IPlayerDataService playerDataService, WaveTableData waveTable, WaveScrollView view)
    {
        _playerDataService = playerDataService;
        _waveTable = waveTable;
        _view = view;
    }

    public void Start()
    {
        _playerDataService.BestWave
            .Subscribe(wave => _view.SetBestWave(wave, _waveTable.MaxWave))
            .AddTo(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
