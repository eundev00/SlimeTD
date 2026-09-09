using System;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UniRx;
using UnityEngine;
using VContainer.Unity;

public class GameplayHudPresenter : IStartable, IDisposable
{
    private readonly GameplayHudView _view;
    private readonly IGameplayService _gameplayService;
    private readonly IResourceLoadService _resourceLoadService;
    private readonly IPublisher<TowerSpawnRequestedEvent> _spawnRequestedPublisher;
    private readonly ISubscriber<GameProgressEvent> _gameProgressSubscriber;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private int _summonCost;
    private bool _ignoreGoldCost;
    private bool _configLoaded;
    private bool _gameEnded;
    private bool _disposed;

    public GameplayHudPresenter(
        GameplayHudView view,
        IGameplayService gameplayService,
        IResourceLoadService resourceLoadService,
        IPublisher<TowerSpawnRequestedEvent> spawnRequestedPublisher,
        ISubscriber<GameProgressEvent> gameProgressSubscriber)
    {
        _view = view;
        _gameplayService = gameplayService;
        _resourceLoadService = resourceLoadService;
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

        LoadConfigAsync().Forget();
    }

    private async UniTaskVoid LoadConfigAsync()
    {
        var config = await _resourceLoadService.LoadAsync<TowerSpawnConfig>(DataKeys.TowerSpawnConfig);

        if (_disposed)
            return;

        if (config == null)
        {
            Debug.Log($"[GameplayHudPresenter] {DataKeys.TowerSpawnConfig} 로드에 실패했습니다.");
            return;
        }

        _summonCost = config.Cost;
        _ignoreGoldCost = config.IgnoreGoldCost;
        _configLoaded = true;

        ApplySummonInteractable();
    }

    private void ApplySummonInteractable()
    {
        if (_disposed)
            return;

        bool affordable = _configLoaded
            && (_ignoreGoldCost || _gameplayService.Info.Gold.Value >= _summonCost);

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
