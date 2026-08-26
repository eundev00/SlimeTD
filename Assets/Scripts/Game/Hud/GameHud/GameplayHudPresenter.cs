using System;
using MessagePipe;
using VContainer.Unity;

public class GameplayHudPresenter : IStartable, IDisposable
{
    private readonly GameplayHudView _view;
    private readonly IPublisher<TowerSpawnRequestedEvent> _spawnRequestedPublisher;

    public GameplayHudPresenter(
        GameplayHudView view,
        IPublisher<TowerSpawnRequestedEvent> spawnRequestedPublisher)
    {
        _view = view;
        _spawnRequestedPublisher = spawnRequestedPublisher;
    }

    public void Start()
    {
        _view.SummonButtonClicked += HandleSummonButtonClicked;
    }

    private void HandleSummonButtonClicked()
    {
        _spawnRequestedPublisher.Publish(new TowerSpawnRequestedEvent());
    }

    public void Dispose()
    {
        if (_view != null)
            _view.SummonButtonClicked -= HandleSummonButtonClicked;
    }
}
