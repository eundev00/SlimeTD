using Cysharp.Threading.Tasks;
using MessagePipe;
using UniRx;
using UnityEngine;

public class GameplayService : IGameplayService
{
    private readonly GameplayInfo _info;
    private readonly IPopupService _popupService;
    private readonly IPlayerDataService _playerDataService;
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
    public TowerCatalog Catalog { get; private set; }
    public SkillCatalog SkillCatalog { get; private set; }
    public WaveTableData WaveTable { get; private set; }
    public SlimeDepthBucketSettings DepthBucketSettings { get; private set; }

    private readonly IResourceLoadService _resourceLoadService;

    public GameplayService(
        IResourceLoadService resourceLoadService,
        IPopupService popupService,
        IPlayerDataService playerDataService,
        ISubscriber<SlimeReachedEndEvent> reachedEndSubscriber,
        ISubscriber<SlimeKilledEvent> killedSubscriber,
        IPublisher<GameProgressEvent> gameProgressPublisher,
        ISubscriber<GameProgressEvent> gameProgressSubscriber)
    {
        _info = new GameplayInfo(0, 0);
        _gameProgressPublisher = gameProgressPublisher;
        _resourceLoadService = resourceLoadService;
        _popupService = popupService;
        _playerDataService = playerDataService;

        reachedEndSubscriber.Subscribe(OnSlimeReachedEnd).AddTo(_disposables);
        killedSubscriber.Subscribe(OnSlimeKilled).AddTo(_disposables);
        gameProgressSubscriber.Subscribe(OnGameProgress).AddTo(_disposables);
    }

    public async UniTask InitializeAsync()
    {
        _info.PreviousBestWave.Value = _playerDataService.BestWave.Value;
        _info.ReachedWave.Value = 0;

        Config =await _resourceLoadService.LoadAsync<GameConfig>(DataKeys.GameConfig);
        if (Config == null)
        {
            Debug.Log($"[GameplayService] {DataKeys.GameConfig} 로드에 실패했습니다.");
            return;
        }

        Catalog = await _resourceLoadService.LoadAsync<TowerCatalog>(DataKeys.TowerCatalog);
        if (Catalog == null)
        {
            Debug.Log($"[GameplayService] {DataKeys.TowerCatalog} 로드에 실패했습니다.");
            return;
        }

        SkillCatalog = await _resourceLoadService.LoadAsync<SkillCatalog>(DataKeys.SkillCatalog);
        if (SkillCatalog == null)
            Debug.Log($"[GameplayService] {DataKeys.SkillCatalog} 로드에 실패했습니다.");

        WaveTable = await _resourceLoadService.LoadAsync<WaveTableData>(DataKeys.WaveEasyTable);

        DepthBucketSettings = await _resourceLoadService.LoadAsync<SlimeDepthBucketSettings>(DataKeys.SlimeDepthBucketSettings);
        if (DepthBucketSettings == null)
            Debug.Log($"[GameplayService] {DataKeys.SlimeDepthBucketSettings} 로드에 실패했습니다.");

        _info.Life.SetValueAndForceNotify(Config.StartingLife);
        _info.Jelly.SetValueAndForceNotify(Config.StartingJelly);
        _summonCount = 0;
        _info.SummonCost.SetValueAndForceNotify(Config.SummonBaseCost);
    }

    public bool TrySpendSummonCost()
    {
        if (Config == null)
            return false;

        int cost = _info.SummonCost.Value;
        if (!Config.IgnoreJellyCost && cost > 0 && !TrySpendJelly(cost))
            return false;

        _summonCount++;
        _info.SummonCost.Value = Config.SummonBaseCost + _summonCount * Config.SummonCostIncrease;
        return true;
    }

    public void SetMaxWave(int maxWave)
    {
        _info.MaxWave.Value = maxWave;
    }

    public bool TrySpendJelly(int amount)
    {
        if (amount <= 0)
            return false;

        if (_info.Jelly.Value < amount)
            return false;

        _info.Jelly.Value -= amount;
        return true;
    }

    private void OnGameProgress(GameProgressEvent e)
    {
        if (e.EventType == GameProgressType.GameOver || e.EventType == GameProgressType.StageCleared)
        {
            if (_gameEnded)
                return;

            _gameEnded = true;
            _info.IsCleared.Value = e.EventType == GameProgressType.StageCleared;

            if (_info.IsCleared.Value)
                ReportWaveReached(e.WaveIndex);

            _popupService.OpenAsync(PrefabKeys.GameResultPopup).Forget();
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

                // 마지막 웨이브는 StageCleared에서만 기록해 완료 표시가 클리어 시에만 켜지게 한다
                ReportWaveReached(e.IsLastWave ? e.WaveIndex - 1 : e.WaveIndex);
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

    // 저장하는 최고 기록과 결과 팝업에 보이는 도달 웨이브가 같은 값이어야 해서 한 곳에서 처리한다
    private void ReportWaveReached(int wave)
    {
        _info.ReachedWave.Value = Mathf.Max(_info.ReachedWave.Value, wave);
        _playerDataService.ReportWaveReached(wave);
    }

    private void OnSlimeReachedEnd(SlimeReachedEndEvent e)
    {
        if (_gameOverPublished || _gameEnded)
            return;

        _info.Life.Value = e.InstantGameOver ? 0 : Mathf.Max(0, _info.Life.Value - e.LifeCost);

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

        _info.Jelly.Value = Mathf.Max(0, _info.Jelly.Value + e.JellyReward);
        Debug.Log($"[GameplayService] 젤리 {_info.Jelly.Value}");

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
