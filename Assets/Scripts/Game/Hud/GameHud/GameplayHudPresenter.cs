using System;
using MessagePipe;
using UniRx;
using VContainer.Unity;

public class GameplayHudPresenter : IStartable, IDisposable
{
    private readonly GameplayHudView _view;
    private readonly IGameplayService _gameplayService;
    private readonly IPublisher<TowerSpawnRequestedEvent> _spawnRequestedPublisher;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    public GameplayHudPresenter(
        GameplayHudView view,
        IGameplayService gameplayService,
        IPublisher<TowerSpawnRequestedEvent> spawnRequestedPublisher)
    {
        _view = view;
        _gameplayService = gameplayService;
        _spawnRequestedPublisher = spawnRequestedPublisher;
    }

    public void Start()
    {
        _view.TestSummonButtonClicked += HandleTestSummonButtonClicked;

        _gameplayService.Info.Jelly
            .Subscribe(_view.SetJelly)
            .AddTo(_disposables);

        _gameplayService.Info.SummonCost
            .Subscribe(_view.SetSummonCost)
            .AddTo(_disposables);
    }

    private void HandleTestSummonButtonClicked()
    {
        _spawnRequestedPublisher.Publish(new TowerSpawnRequestedEvent(_gameplayService.Config?.TestTower));
    }

    public void Dispose()
    {
        if (_view != null)
        {
            _view.TestSummonButtonClicked -= HandleTestSummonButtonClicked;
        }

        _disposables.Dispose();
    }
}
