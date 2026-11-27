using Cysharp.Threading.Tasks;
using MessagePipe;
using UniRx;
using UnityEngine;

public class GameplayService : IGameplayService
{
    private readonly GameplayInfo _info;
    private readonly IPopupService _popupService;
    private readonly ISceneLoader _sceneLoader;
    private readonly IAccountService _accountService;
    private readonly ISingleBattleService _singleBattleService;
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
    public StageData Stage { get; private set; }

    private readonly IResourceLoadService _resourceLoadService;

    public GameplayService(
        IResourceLoadService resourceLoadService,
        IPopupService popupService,
        ISceneLoader sceneLoader,
        IAccountService accountService,
        ISingleBattleService singleBattleService,
        ISubscriber<SlimeReachedEndEvent> reachedEndSubscriber,
        ISubscriber<SlimeKilledEvent> killedSubscriber,
        IPublisher<GameProgressEvent> gameProgressPublisher,
        ISubscriber<GameProgressEvent> gameProgressSubscriber)
    {
        _info = new GameplayInfo(0, 0);
        _gameProgressPublisher = gameProgressPublisher;
        _resourceLoadService = resourceLoadService;
        _popupService = popupService;
        _sceneLoader = sceneLoader;
        _accountService = accountService;
        _singleBattleService = singleBattleService;

        reachedEndSubscriber.Subscribe(OnSlimeReachedEnd).AddTo(_disposables);
        killedSubscriber.Subscribe(OnSlimeKilled).AddTo(_disposables);
        gameProgressSubscriber.Subscribe(OnGameProgress).AddTo(_disposables);
    }

    public async UniTask InitializeAsync(string stageKey = DataKeys.Stage_SingleForest01)
    {
        _info.PreviousBestWave.Value = _accountService.BestWave.Value;
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

        Stage = await _resourceLoadService.LoadAsync<StageData>(stageKey);
        if (Stage == null)
            Debug.Log($"[GameplayService] {stageKey} 로드에 실패했습니다.");

        _info.Life.SetValueAndForceNotify(Stage != null ? Stage.StartingLife : 0);
        _info.Gold.SetValueAndForceNotify(Stage != null ? Stage.StartingGold : 0);
        _summonCount = 0;
        _info.SummonCost.SetValueAndForceNotify(Config.SummonBaseCost);
    }

    public bool TrySpendSummonCost()
    {
        if (Config == null || _gameEnded)
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

    public void SetLife(int life)
    {
        _info.Life.Value = Mathf.Max(0, life);
    }

    public void SetGold(int gold, int summonCost)
    {
        _info.Gold.Value = Mathf.Max(0, gold);
        _info.SummonCost.Value = summonCost;
    }

    public void ReportReachedWave(int wave)
    {
        ReportWaveReached(wave);
    }

    public void AddSkillGold(int amount, Vector2Int cell)
    {
        if (amount <= 0 || _gameEnded)
            return;

        _info.Gold.Value += amount;
        _singleBattleService.RecordSkillGold(cell, amount);
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

    // 서버 응답을 받은 뒤에 결과 팝업(정상) 또는 에러 팝업을 연다
    private async UniTaskVoid FinishBattleAsync()
    {
        var result = await _singleBattleService.FinishAsync(_info.IsCleared.Value, _info.ReachedWave.Value, _info.Life.Value, _info.Gold.Value);
        await BattleFinishPopups.OpenAsync(result, _popupService, _sceneLoader);
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

            FinishBattleAsync().Forget();
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

    private void ReportWaveReached(int wave)
    {
        _info.ReachedWave.Value = Mathf.Max(_info.ReachedWave.Value, wave);
    }

    private void OnSlimeReachedEnd(SlimeReachedEndEvent e)
    {
        if (_gameOverPublished || _gameEnded)
            return;

        _singleBattleService.RecordArrival(e.SpawnNumber);
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

        _singleBattleService.RecordKill(e.SpawnNumber);
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
        _singleBattleService.Abandon();
        _disposables.Dispose();
        _info.Dispose();
    }
}
