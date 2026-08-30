using Cysharp.Threading.Tasks;
using MessagePipe;
using UniRx;
using UnityEngine;

// 게임플레이 수치 로직 담당(MVP의 Model). 상태는 GameplayInfo가 들고, 변경은 여기서만 한다.
// 기존 LifeService를 대체 — 라이프 차감/게임오버 로직은 그대로 유지하고 골드 처리를 통합했다.
public class GameplayService : IGameplayService
{
    private readonly GameplayInfo _info;
    private readonly IPublisher<GameProgressEvent> _gameProgressPublisher;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private bool _gameOverPublished;

    private int _aliveCount;
    private bool _spawnFinished;
    private int _currentWaveIndex;

    public GameplayInfo Info => _info;

    public GameplayService(
        IResourceLoadService resourceLoadService,
        ISubscriber<SlimeReachedEndEvent> reachedEndSubscriber,
        ISubscriber<SlimeKilledEvent> killedSubscriber,
        IPublisher<GameProgressEvent> gameProgressPublisher,
        ISubscriber<GameProgressEvent> gameProgressSubscriber)
    {
        _info = new GameplayInfo(0, 0);
        _gameProgressPublisher = gameProgressPublisher;

        reachedEndSubscriber.Subscribe(OnSlimeReachedEnd).AddTo(_disposables);
        killedSubscriber.Subscribe(OnSlimeKilled).AddTo(_disposables);
        gameProgressSubscriber.Subscribe(OnGameProgress).AddTo(_disposables);

        LoadConfigAsync(resourceLoadService).Forget();
    }

    // 시작 수치는 로드가 끝나야 정해진다. ReactiveProperty라 HUD는 값이 들어오는 순간 갱신된다.
    private async UniTaskVoid LoadConfigAsync(IResourceLoadService resourceLoadService)
    {
        var config = await resourceLoadService.LoadAsync<GameConfig>(DataKeys.GameConfig);
        if (config == null)
        {
            Debug.Log($"[GameplayService] {DataKeys.GameConfig} 로드에 실패했습니다.");
            return;
        }

        _info.Life.Value = config.StartingLife;
        _info.Gold.Value = config.StartingGold;
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
        switch (e.EventType)
        {
            // 웨이브가 겹치므로 덮어쓰면 이전 웨이브 생존분이 카운터에서 사라진다.
            case GameProgressType.WaveStarted:
                _currentWaveIndex = e.WaveIndex;
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
        if (_gameOverPublished)
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
