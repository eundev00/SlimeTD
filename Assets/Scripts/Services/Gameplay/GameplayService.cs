using Cysharp.Threading.Tasks;
using MessagePipe;
using UniRx;
using UnityEngine;

public class GameplayService : IGameplayService
{
    private readonly GameplayInfo _info;
    private readonly IPublisher<GameProgressEvent> _gameProgressPublisher;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private bool _gameOverPublished;
    private bool _gameEnded;

    private int _aliveCount;
    private bool _spawnFinished;
    private int _currentWaveIndex;
    private int _summonCount;

    public GameplayInfo Info => _info;
    public GameConfig Config { get; private set; }
    public TowerTierTable TierTable { get; private set; }
    public WaveTableData WaveTable { get; private set; }

    private readonly IResourceLoadService _resourceLoadService;

    public GameplayService(
        IResourceLoadService resourceLoadService,
        ISubscriber<SlimeReachedEndEvent> reachedEndSubscriber,
        ISubscriber<SlimeKilledEvent> killedSubscriber,
        IPublisher<GameProgressEvent> gameProgressPublisher,
        ISubscriber<GameProgressEvent> gameProgressSubscriber)
    {
        _info = new GameplayInfo(0, 0);
        _gameProgressPublisher = gameProgressPublisher;
        _resourceLoadService = resourceLoadService;

        reachedEndSubscriber.Subscribe(OnSlimeReachedEnd).AddTo(_disposables);
        killedSubscriber.Subscribe(OnSlimeKilled).AddTo(_disposables);
        gameProgressSubscriber.Subscribe(OnGameProgress).AddTo(_disposables);
    }

    public async UniTask InitializeAsync()
    {
        Config = await _resourceLoadService.LoadAsync<GameConfig>(DataKeys.GameConfig);
        if (Config == null)
        {
            Debug.Log($"[GameplayService] {DataKeys.GameConfig} 로드에 실패했습니다.");
            return;
        }

        TierTable = await _resourceLoadService.LoadAsync<TowerTierTable>(DataKeys.TowerTierTable);
        if (TierTable == null)
        {
            Debug.Log($"[GameplayService] {DataKeys.TowerTierTable} 로드에 실패했습니다.");
            return;
        }

        WaveTable = await _resourceLoadService.LoadAsync<WaveTableData>(DataKeys.WaveEasyTable);

        _info.Life.SetValueAndForceNotify(Config.StartingLife);
        _info.Gold.SetValueAndForceNotify(Config.StartingGold);
        _summonCount = 0;
        _info.SummonCost.SetValueAndForceNotify(Config.SummonBaseCost);
    }

    public bool TrySpendSummonCost()
    {
        if (Config == null)
            return false;

        int cost = _info.SummonCost.Value;
        if (!Config.IgnoreGoldCost && cost > 0 && !TrySpendGold(cost))
            return false;

        _summonCount++;
        _info.SummonCost.Value = Config.SummonBaseCost + _summonCount * Config.SummonCostIncrease;
        return true;
    }

    public void SetMaxWave(int maxWave)
    {
        _info.MaxWave.Value = maxWave;
    }

    public bool TrySpendGold(int amount)
    {
        if (amount <= 0)
            return false;

        if (_info.Gold.Value < amount)
            return false;

        _info.Gold.Value -= amount;
        return true;
    }

    private void OnGameProgress(GameProgressEvent e)
    {
        if (e.EventType == GameProgressType.GameOver || e.EventType == GameProgressType.StageCleared)
        {
            _gameEnded = true;
            return;
        }

        if (_gameEnded)
            return;

        switch (e.EventType)
        {
            // 웨이브가 겹치므로 덮어쓰면 이전 웨이브 생존분이 카운터에서 사라진다.
            case GameProgressType.WaveStarted:
                _currentWaveIndex = e.WaveIndex;
                _info.CurrentWave.Value = e.WaveIndex;
                _aliveCount += e.SlimeCount;
                Debug.Log($"[GameplayService] 웨이브 {_currentWaveIndex} 시작, 누적 슬라임: {_aliveCount}");
                break;

            case GameProgressType.WaveSpawnFinished:
                if (e.IsLastWave)
                    _spawnFinished = true;
                Debug.Log($"[GameplayService] 웨이브 {e.WaveIndex} 스폰 완료, 남은 슬라임: {_aliveCount}");
                CheckWaveCleared();
                break;
        }
    }

    private void OnSlimeReachedEnd(SlimeReachedEndEvent e)
    {
        if (_gameOverPublished || _gameEnded)
            return;

        _info.Life.Value = Mathf.Max(0, _info.Life.Value - e.LifeCost);
        Debug.Log($"[GameplayService] 라이프 {_info.Life.Value}");

        if (_info.Life.Value <= 0)
        {
            _gameOverPublished = true;
            _gameProgressPublisher.Publish(new GameProgressEvent(GameProgressType.GameOver));
            Debug.Log("[GameplayService] 게임오버");
        }

        OnSlimeRemoved();
    }

    private void OnSlimeKilled(SlimeKilledEvent e)
    {
        if (_gameEnded)
            return;

        _info.Gold.Value = Mathf.Max(0, _info.Gold.Value + e.GoldReward);
        Debug.Log($"[GameplayService] 골드 {_info.Gold.Value}");

        OnSlimeRemoved();
    }

    private void OnSlimeRemoved()
    {
        _aliveCount = Mathf.Max(0, _aliveCount - 1);
        CheckWaveCleared();
    }

    private void CheckWaveCleared()
    {
        if (_spawnFinished && _aliveCount <= 0)
        {
            _gameProgressPublisher.Publish(new GameProgressEvent(GameProgressType.WaveCleared, _currentWaveIndex));
            Debug.Log($"[GameplayService] 웨이브 {_currentWaveIndex} 클리어");
        }
    }

    public void Dispose()
    {
        _disposables.Dispose();
        _info.Dispose();
    }
}
