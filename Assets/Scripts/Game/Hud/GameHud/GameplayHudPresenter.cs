using System;
using MessagePipe;
using UniRx;
using VContainer.Unity;

public class GameplayHudPresenter : IStartable, IDisposable
{
    private readonly GameplayHudView _view;
    private readonly IGameplayService _gameplayService;
    private readonly IPublisher<TowerSpawnRequestedEvent> _spawnRequestedPublisher;
    private readonly ISubscriber<GameProgressEvent> _gameProgressSubscriber;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private bool _gameEnded;
    private bool _disposed;

    public GameplayHudPresenter(
        GameplayHudView view,
        IGameplayService gameplayService,
        IPublisher<TowerSpawnRequestedEvent> spawnRequestedPublisher,
        ISubscriber<GameProgressEvent> gameProgressSubscriber)
    {
        _view = view;
        _gameplayService = gameplayService;
        _spawnRequestedPublisher = spawnRequestedPublisher;
        _gameProgressSubscriber = gameProgressSubscriber;
    }

    public void Start()
    {
        _view.SummonButtonClicked += HandleSummonButtonClicked;
        _view.SetSummonButtonInteractable(false);

        _gameplayService.Info.Gold
            .Subscribe(gold =>
            {
                _view.SetGold(gold);
                ApplySummonInteractable();
            })
            .AddTo(_disposables);

        _gameProgressSubscriber.Subscribe(HandleGameProgress).AddTo(_disposables);
    }

    private void ApplySummonInteractable()
    {
        if (_disposed)
            return;

        var gameConfig = _gameplayService.Config;
        var towerConfig = _gameplayService.TowerConfig;
        if (gameConfig == null || towerConfig == null)
            return;

        bool affordable = gameConfig.IgnoreGoldCost
            || _gameplayService.Info.Gold.Value >= towerConfig.Cost;

        _view.SetSummonButtonInteractable(!_gameEnded && affordable);
    }

    private void HandleGameProgress(GameProgressEvent e)
    {
        if (e.EventType != GameProgressType.GameOver && e.EventType != GameProgressType.StageCleared)
            return;

        _gameEnded = true;
        ApplySummonInteractable();
    }

    private void HandleSummonButtonClicked()
    {
        _spawnRequestedPublisher.Publish(new TowerSpawnRequestedEvent());
    }

    public void Dispose()
    {
        _disposed = true;

        if (_view != null)
            _view.SummonButtonClicked -= HandleSummonButtonClicked;

        _disposables.Dispose();
    }
}
