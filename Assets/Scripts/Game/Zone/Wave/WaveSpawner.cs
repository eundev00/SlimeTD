using System;
using System.Collections.Generic;
using MessagePipe;
using Services.PoolService;
using Services.UpdateService;
using UniRx;
using UnityEngine;
using VContainer;

public class WaveSpawner : MonoBehaviour, IFixedUpdatable
{
    private const int TicksPerSecond = 30;

    private enum ScheduleKind { WaveStart, Spawn, WaveSpawnFinished }

    private readonly struct ScheduleEntry
    {
        public readonly int Tick;
        public readonly ScheduleKind Kind;
        public readonly int Wave;
        public readonly SpawnPlan Plan;
        public readonly int Count;
        public readonly bool IsLastWave;

        public ScheduleEntry(int tick, ScheduleKind kind, int wave, SpawnPlan plan, int count, bool isLastWave)
        {
            Tick = tick;
            Kind = kind;
            Wave = wave;
            Plan = plan;
            Count = count;
            IsLastWave = isLastWave;
        }
    }

    // 웨이브가 겹쳐 스폰되므로 동시 생존 수를 이 창(window) 크기로 근사한다.
    [SerializeField] private int _overlapWaveWindow = 3;
    [NotNull][SerializeField] private Transform _spawnRoot;

    private StageData _stage;
    private IReadOnlyList<MapPath> _paths;
    private int _spawnCount;
    private IGameObjectPoolService _poolService;
    private IPublisher<GameProgressEvent> _gameProgressPublisher;
    private ISubscriber<GameProgressEvent> _gameProgressSubscriber;
    private IGameplayService _gameplayService;
    private ISlimePrefabProvider _slimePrefabProvider;
    private ISingleBattleService _singleBattleService;
    private IUpdateSubscriptionService _updateService;

    private readonly List<ScheduleEntry> _schedule = new List<ScheduleEntry>();
    private int _scheduleIndex;
    private int _tick;
    private bool _running;
    private bool _lastWaveSpawnFinished;

    private CompositeDisposable _disposables;
    private bool _gameOver;

    [Inject]
    public void Construct(
        IGameObjectPoolService poolService,
        IPublisher<GameProgressEvent> gameProgressPublisher,
        ISubscriber<GameProgressEvent> gameProgressSubscriber,
        IGameplayService gameplayService,
        ISlimePrefabProvider slimePrefabProvider,
        ISingleBattleService singleBattleService,
        IUpdateSubscriptionService updateService)
    {
        _updateService = updateService;
        _singleBattleService = singleBattleService;
        _poolService = poolService;
        _gameProgressPublisher = gameProgressPublisher;
        _gameProgressSubscriber = gameProgressSubscriber;
        _gameplayService = gameplayService;
        _slimePrefabProvider = slimePrefabProvider;
    }

    public void Initialize(StageData stage, IReadOnlyList<MapPath> paths)
    {
        _stage = stage;
        _paths = paths;
        _spawnCount = 0;

        if (_paths == null || _paths.Count == 0)
        {
            Debug.Log("[WaveSpawner] 슬라임 경로가 없습니다.", this);
            return;
        }

        if (_stage == null || _stage.FinalWave <= 0)
        {
            Debug.Log("[WaveSpawner] 스테이지 데이터가 없거나 웨이브가 비어 있습니다.", this);
            return;
        }

        _gameplayService.SetMaxWave(_stage.FinalWave);

        _gameOver = false;

        _disposables = new CompositeDisposable();
        _gameProgressSubscriber.Subscribe(evt =>
        {
            if (evt.EventType == GameProgressType.GameOver)
                OnGameOver();
            else if (evt.EventType == GameProgressType.WaveCleared)
                OnWaveCleared();
        }).AddTo(_disposables);

        PreparePools();

        if (_gameplayService.Config.AutoStartWave)
            StartWaves();
    }

    private void OnDestroy()
    {
        _disposables?.Dispose();
        _disposables = null;

        StopWaves();
    }

    private void PreparePools()
    {
        int finalWave = _stage.FinalWave;
        var plans = new List<SpawnPlan>();
        var perWave = new List<Dictionary<GameObject, int>>(finalWave + 1) { null };

        for (int waveIndex = 1; waveIndex <= finalWave; waveIndex++)
        {
            WaveResolver.Resolve(_stage, waveIndex, plans, false);

            var demand = new Dictionary<GameObject, int>();
            foreach (var plan in plans)
            {
                if (!_slimePrefabProvider.TryGet(plan.SlimeData, out var prefab))
                    continue;

                demand.TryGetValue(prefab, out int current);
                demand[prefab] = current + 1;
            }

            perWave.Add(demand);
        }

        int window = Mathf.Max(1, _overlapWaveWindow);
        var peak = new Dictionary<GameObject, int>();
        var windowSum = new Dictionary<GameObject, int>();

        for (int waveIndex = 1; waveIndex <= finalWave; waveIndex++)
        {
            windowSum.Clear();

            for (int k = waveIndex; k < waveIndex + window && k <= finalWave; k++)
            {
                foreach (var kvp in perWave[k])
                {
                    windowSum.TryGetValue(kvp.Key, out int current);
                    windowSum[kvp.Key] = current + kvp.Value;
                }
            }

            foreach (var kvp in windowSum)
            {
                peak.TryGetValue(kvp.Key, out int current);
                if (kvp.Value > current)
                    peak[kvp.Key] = kvp.Value;
            }
        }

        foreach (var kvp in peak)
        {
            int initialSize = Mathf.Clamp(kvp.Value, 8, 256);
            int maxSize = Mathf.CeilToInt(kvp.Value * 1.5f) + 8;

            _poolService.CreatePool(kvp.Key, initialSize, maxSize);
            Debug.Log($"[WaveSpawner] 풀 생성: {kvp.Key.name}, 초기={initialSize}, 최대={maxSize}");
        }
    }

    // 서버 전투 코어와 같은 일정: 1틱 = 고정 업데이트 1번 (1/30초). 웨이브 일정은 시작할 때 틱으로 미리 정한다
    private void BuildSchedule()
    {
        _schedule.Clear();
        _scheduleIndex = 0;
        _tick = 0;
        _lastWaveSpawnFinished = false;

        int tick = 0;
        for (int waveIndex = 1; waveIndex <= _stage.FinalWave; waveIndex++)
        {
            var plans = new List<SpawnPlan>();
            WaveResolver.Resolve(_stage, waveIndex, plans, true);
            if (plans.Count == 0)
            {
                Debug.Log($"[WaveSpawner] 웨이브 {waveIndex} 스폰 목록이 비어 건너뜁니다.", this);
                continue;
            }

            bool isLastWave = waveIndex == _stage.FinalWave;
            tick += SecondsToTicks(_stage.InterWaveDelay);
            _schedule.Add(new ScheduleEntry(tick, ScheduleKind.WaveStart, waveIndex, default, plans.Count, isLastWave));
            Debug.Log($"[WaveSpawner] 웨이브 {waveIndex} 슬라임 {plans.Count}마리 [{DescribePlans(plans)}]");

            foreach (var plan in plans)
            {
                tick += SecondsToTicks(plan.DelayBefore);
                _schedule.Add(new ScheduleEntry(tick, ScheduleKind.Spawn, waveIndex, plan, 0, isLastWave));
                tick += SecondsToTicks(plan.SpawnInterval);
            }

            _schedule.Add(new ScheduleEntry(tick, ScheduleKind.WaveSpawnFinished, waveIndex, default, plans.Count, isLastWave));
        }
    }

    private static int SecondsToTicks(float seconds)
    {
        return seconds <= 0f ? 0 : (int)Math.Round(seconds * TicksPerSecond, MidpointRounding.AwayFromZero);
    }

    private void StartWaves()
    {
        BuildSchedule();
        _singleBattleService.MarkStarted();
        _updateService.RegisterFixedUpdatable(this);
        _running = true;
    }

    private void StopWaves()
    {
        if (!_running)
            return;

        _running = false;
        _updateService.UnregisterFixedUpdatable(this);
    }

    public void ManagedFixedUpdate()
    {
        if (!_running || _gameOver)
            return;

        _tick++;
        _singleBattleService.AdvanceTick();

        while (_scheduleIndex < _schedule.Count && _schedule[_scheduleIndex].Tick <= _tick)
        {
            var entry = _schedule[_scheduleIndex++];
            switch (entry.Kind)
            {
                case ScheduleKind.WaveStart:
                    _gameProgressPublisher.Publish(new GameProgressEvent(GameProgressType.WaveStarted, entry.Wave, entry.Count, entry.IsLastWave));
                    Debug.Log($"[WaveSpawner] 웨이브 {entry.Wave} 시작 (틱 {_tick})");
                    break;
                case ScheduleKind.Spawn:
                    SpawnOne(entry.Plan);
                    break;
                case ScheduleKind.WaveSpawnFinished:
                    if (entry.IsLastWave)
                        _lastWaveSpawnFinished = true;
                    _gameProgressPublisher.Publish(new GameProgressEvent(GameProgressType.WaveSpawnFinished, entry.Wave, entry.Count, entry.IsLastWave));
                    break;
            }

            if (_gameOver || !_running)
                return;
        }
    }

    // 마지막 웨이브 스폰이 끝난 뒤 슬라임이 모두 처리되면 클리어다. 처치는 틱 사이에 일어나므로 받은 즉시 끝낸다
    private void OnWaveCleared()
    {
        if (!_running || !_lastWaveSpawnFinished || _gameOver)
            return;

        StopWaves();
        _gameProgressPublisher.Publish(new GameProgressEvent(GameProgressType.StageCleared, _stage.FinalWave));
        Debug.Log($"[WaveSpawner] 스테이지 클리어 (틱 {_tick})");
    }
    private static string DescribePlans(List<SpawnPlan> plans)
    {
        var builder = new System.Text.StringBuilder();
        foreach (var plan in plans)
        {
            if (builder.Length > 0)
                builder.Append(", ");

            builder.Append(plan.SlimeData.name).Append("(hp").Append(plan.Health).Append(')');
        }

        return builder.ToString();
    }

    private void SpawnOne(SpawnPlan plan)
    {
        var slimeData = plan.SlimeData;

        if (!_slimePrefabProvider.TryGet(slimeData, out var prefab))
        {
            Debug.Log($"[WaveSpawner] 슬라임 프리팹이 없습니다: {slimeData.CodeName}", this);
            return;
        }

        // 입구가 여럿이면 무리 안에서 번갈아 내보내 입구마다 같은 수가 나온다
        var path = _paths[_spawnCount % _paths.Count].Spline;
        _spawnCount++;

        var obj = _poolService.Get(prefab);
        if (obj == null)
            return;

        obj.transform.SetParent(_spawnRoot, false);
        obj.transform.position = path.EvaluatePosition(0f);

        var slime = obj.GetComponent<BaseSlime>();
        if (slime == null)
        {
            Debug.Log("[WaveSpawner] 슬라임 프리팹에 BaseSlime 컴포넌트가 없습니다.", obj);
            return;
        }

        slime.Initialize(path, slimeData, plan.Health, plan.GoldReward, _spawnCount);
    }

    private void OnGameOver()
    {
        _gameOver = true;
        StopWaves();
    }
}
