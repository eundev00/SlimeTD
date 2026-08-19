using System;
using MessagePipe;
using UniRx;

public class HudViewPresenter : IDisposable
{
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    public HudViewPresenter(
        IGameplayService gameplayService,
        ISubscriber<WaveStartedEvent> waveStartedSubscriber,
        HudView hud)
    {
        gameplayService.Info.Gold.Subscribe(hud.SetGold).AddTo(_disposables);
        gameplayService.Info.Life.Subscribe(hud.SetLife).AddTo(_disposables);
        waveStartedSubscriber.Subscribe(e => hud.SetWave(e.WaveIndex)).AddTo(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
