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

        var info = _gameplayService.Info;

        info.Gold.Subscribe(_view.SetGold).AddTo(_disposables);
        info.Jelly.Subscribe(_view.SetJelly).AddTo(_disposables);
        info.SummonCost.Subscribe(_view.SetSummonCost).AddTo(_disposables);

        info.Gold
            .CombineLatest(info.SummonCost, IsAffordable)
            .DistinctUntilChanged()
            .Subscribe(_view.SetSummonAffordable)
            .AddTo(_disposables);
    }

    private bool IsAffordable(int gold, int cost)
    {
        var config = _gameplayService.Config;
        return (config != null && config.IgnoreGoldCost) || gold >= cost;
    }

    private void HandleTestSummonButtonClicked()
    {
        // 테스트 소환은 스킬을 다 보려고 최대 레벨로 낸다
        _spawnRequestedPublisher.Publish(new TowerSpawnRequestedEvent(_gameplayService.Config?.TestTower, TowerLevelService.MaxLevel));
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
