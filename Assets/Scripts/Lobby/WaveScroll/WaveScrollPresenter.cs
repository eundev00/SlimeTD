using System;
using UniRx;
using VContainer.Unity;

public class WaveScrollPresenter : IStartable, IDisposable
{
    private readonly IAccountService _accountService;
    private readonly StageData _stage;
    private readonly WaveScrollView _view;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    public WaveScrollPresenter(IAccountService accountService, StageData stage, WaveScrollView view)
    {
        _accountService = accountService;
        _stage = stage;
        _view = view;
    }

    public void Start()
    {
        _accountService.BestWave
            .Subscribe(wave => _view.SetBestWave(wave, _stage.FinalWave))
            .AddTo(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
