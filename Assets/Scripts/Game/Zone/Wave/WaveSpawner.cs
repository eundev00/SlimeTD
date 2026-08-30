using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using Services.PoolService;
using UniRx;
using UnityEngine;
using UnityEngine.Splines;
using VContainer;

public class WaveSpawner : MonoBehaviour
{
    private readonly struct SpawnPlan
    {
        public readonly SlimeData SlimeData;
        public readonly int Health;
        public readonly float SpawnInterval;

        public SpawnPlan(SlimeData slimeData, int health, float spawnInterval)
        {
            SlimeData = slimeData;
            Health = health;
            SpawnInterval = spawnInterval;
        }
    }

    [NotNull][SerializeField] private SplineContainer _splineContainer;
    [SerializeField] private bool _autoStart = true;

    private WaveTableData _waveTable;
    private Transform _spawnRoot;
    private IGameObjectPoolService _poolService;
    private IPublisher<GameProgressEvent> _gameProgressPublisher;
    private ISubscriber<GameProgressEvent> _gameProgressSubscriber;

    private CompositeDisposable _disposables;
    private CancellationTokenSource _spawnCts;
    private bool _waveClearedReceived;
    private bool _gameOver;

    [Inject]
    public void Construct(
        IGameObjectPoolService poolService,
        IPublisher<GameProgressEvent> gameProgressPublisher,
        ISubscriber<GameProgressEvent> gameProgressSubscriber)
    {
        _poolService = poolService;
        _gameProgressPublisher = gameProgressPublisher;
        _gameProgressSubscriber = gameProgressSubscriber;
    }

    public void Initialize(WaveTableData waveTable, Transform spawnRoot)
    {
        if (_poolService == null || _gameProgressPublisher == null || _gameProgressSubscriber == null)
        {
            Debug.Log("[WaveSpawner] 의존성이 주입되지 않아 웨이브를 시작할 수 없습니다.", this);
            return;
        }

        _waveTable = waveTable;
        _spawnRoot = spawnRoot;

        if (_splineContainer == null)
        {
            Debug.Log("[WaveSpawner] _splineContainer가 연결되지 않았습니다.", this);
            return;
        }

        if (_waveTable == null || _waveTable.MaxWave <= 0)
        {
            Debug.Log("[WaveSpawner] _waveTable이 비어 있거나 MaxWave가 0 이하입니다.", this);
            return;
        }

        _gameOver = false;
        _waveClearedReceived = false;

        _disposables = new CompositeDisposable();
        _gameProgressSubscriber.Subscribe(evt =>
        {
            if (evt.EventType == GameProgressType.GameOver)
                OnGameOver();
            else if (evt.EventType == GameProgressType.WaveCleared)
                _waveClearedReceived = true;
        }).AddTo(_disposables);

        _spawnCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

        PreparePools();

        if (_autoStart)
            RunAllWavesAsync().Forget();
    }

    private void OnDestroy()
    {
        _disposables?.Dispose();
        _disposables = null;

        _spawnCts?.Cancel();
        _spawnCts?.Dispose();
        _spawnCts = null;
    }

    private List<SpawnPlan> ResolveWave(int waveIndex)
    {
        var plans = new List<SpawnPlan>();

        if (_waveTable.ManualWave != null)
        {
            foreach (var entry in _waveTable.ManualWave)
            {
                if (entry == null || entry.WaveIndex != waveIndex)
                    continue;

                AddPlans(plans, entry, 1f, 1f, 0);
            }
        }

        if (plans.Count == 0 && _waveTable.AutoWave != null && _waveTable.AutoWave.Length > 0)
        {
            float countMultiplier = waveIndex * _waveTable.CountRate;
            float healthMultiplier = 1f + waveIndex * _waveTable.HealthRate;

            var entry = _waveTable.AutoWave[UnityEngine.Random.Range(0, _waveTable.AutoWave.Length)];
            AddPlans(plans, entry, countMultiplier, healthMultiplier, _waveTable.MaxSlimeCount);
        }

        if (_waveTable.BossWave != null)
        {
            foreach (var entry in _waveTable.BossWave)
            {
                if (entry == null || entry.WaveIndex != waveIndex)
                    continue;

                if (entry.WaveIndex > _waveTable.MaxWave)
                    continue;

                AddPlans(plans, entry, 1f, 1f, 0);
            }
        }

        return plans;
    }

    private static void AddPlans(List<SpawnPlan> plans, SpawnEntry entry, float countMultiplier, float healthMultiplier, int maxCount)
    {
        if (entry == null || entry.SlimeDatas == null || entry.SlimeDatas.Length == 0)
            return;

        int baseCount = entry.SlimeDatas.Length;
        int totalCount = Mathf.Max(1, Mathf.RoundToInt(baseCount * countMultiplier));

        if (maxCount > 0)
            totalCount = Mathf.Min(totalCount, maxCount);

        for (int i = 0; i < totalCount; i++)
        {
            var slimeData = entry.SlimeDatas[i % baseCount];
            if (slimeData == null)
                continue;

            int health = Mathf.Max(1, Mathf.RoundToInt(slimeData.BaseHealth * healthMultiplier));
            plans.Add(new SpawnPlan(slimeData, health, entry.SpawnInterval));
        }
    }

    // TODO: 웨이브가 겹쳐 스폰되므로 동시 생존 수 기준으로 풀 크기를 재산정할 것
    private void PreparePools()
    {
        var prefabCounts = new Dictionary<GameObject, int>();

        for (int waveIndex = 1; waveIndex <= _waveTable.MaxWave; waveIndex++)
        {
            var plans = new List<SpawnPlan>();

            if (_waveTable.ManualWave != null)
            {
                foreach (var entry in _waveTable.ManualWave)
                {
                    if (entry != null && entry.WaveIndex == waveIndex)
                        AddPlans(plans, entry, 1f, 1f, 0);
                }
            }

            if (plans.Count == 0 && _waveTable.AutoWave != null)
            {
                float countMultiplier = waveIndex * _waveTable.CountRate;

                foreach (var entry in _waveTable.AutoWave)
                {
                    AddPlans(plans, entry, countMultiplier, 1f, _waveTable.MaxSlimeCount);
                }
            }

            if (_waveTable.BossWave != null)
            {
                foreach (var entry in _waveTable.BossWave)
                {
                    if (entry != null && entry.WaveIndex == waveIndex && entry.WaveIndex <= _waveTable.MaxWave)
                        AddPlans(plans, entry, 1f, 1f, 0);
                }
            }

            foreach (var plan in plans)
            {
                var prefab = plan.SlimeData.Prefab;
                if (prefab == null)
                    continue;

                prefabCounts.TryGetValue(prefab, out int current);
                prefabCounts[prefab] = current + 1;
            }
        }

        foreach (var kvp in prefabCounts)
        {
            int totalCount = kvp.Value;
            int initialSize = Mathf.Max(totalCount, 10);
            int maxSize = Mathf.CeilToInt(totalCount * 2f);

            _poolService.CreatePool(kvp.Key, initialSize, maxSize);
            Debug.Log($"[WaveSpawner] 풀 생성: {kvp.Key.name}, 초기={initialSize}, 최대={maxSize}");
        }
    }

    private async UniTaskVoid RunAllWavesAsync()
    {
        var token = _spawnCts.Token;

        try
        {
            for (int waveIndex = 1; waveIndex <= _waveTable.MaxWave; waveIndex++)
            {
                await RunWaveAsync(waveIndex, token);
            }

            if (_gameOver)
                return;

            _gameProgressPublisher.Publish(new GameProgressEvent(GameProgressType.StageCleared, _waveTable.MaxWave));
            Debug.Log("[WaveSpawner] 스테이지 클리어");
        }
        catch (OperationCanceledException)
        {
            // 게임오버 또는 파괴로 스폰 루프 취소 시 정상 종료.
        }
    }

    private async UniTask RunWaveAsync(int waveIndex, CancellationToken token)
    {
        var plans = ResolveWave(waveIndex);
        if (plans.Count == 0)
        {
            Debug.Log($"[WaveSpawner] 웨이브 {waveIndex} 스폰 목록이 비어 건너뜁니다.", this);
            return;
        }

        float startDelay = plans[0].SpawnInterval;
        if (startDelay > 0f)
            await UniTask.Delay(TimeSpan.FromSeconds(startDelay), cancellationToken: token);

        bool isLastWave = waveIndex == _waveTable.MaxWave;
        int totalSlimeCount = plans.Count;

        _waveClearedReceived = false;
        _gameProgressPublisher.Publish(
            new GameProgressEvent(GameProgressType.WaveStarted, waveIndex, totalSlimeCount, isLastWave));
        Debug.Log($"[WaveSpawner] 웨이브 {waveIndex} 시작, 슬라임 {totalSlimeCount}마리 [{DescribePlans(plans)}]");

        foreach (var plan in plans)
        {
            SpawnOne(plan.SlimeData, plan.Health);

            if (plan.SpawnInterval > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(plan.SpawnInterval), cancellationToken: token);
        }

        _gameProgressPublisher.Publish(
            new GameProgressEvent(GameProgressType.WaveSpawnFinished, waveIndex, totalSlimeCount, isLastWave));
        Debug.Log($"[WaveSpawner] 웨이브 {waveIndex} 스폰 완료");

        // 일반 웨이브는 스폰이 끝나면 곧바로 다음 웨이브로 넘어간다. 슬라임 전멸을 기다리는 건
        // 마지막 웨이브뿐이며, 이 대기가 곧 스테이지 클리어 판정이다.
        if (isLastWave)
            await UniTask.WaitUntil(() => _waveClearedReceived, cancellationToken: token);
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

    private void SpawnOne(SlimeData slimeData, int health)
    {
        var prefab = slimeData.Prefab;
        if (prefab == null)
        {
            Debug.Log($"[WaveSpawner] {slimeData.name}에 프리팹이 연결되지 않았습니다.", this);
            return;
        }

        var obj = _poolService.Get(prefab);
        if (obj == null)
            return;

        obj.transform.SetParent(_spawnRoot, false);
        obj.transform.position = _splineContainer.EvaluatePosition(0f);

        var slime = obj.GetComponent<BaseSlime>();
        if (slime == null)
        {
            Debug.Log("[WaveSpawner] 슬라임 프리팹에 BaseSlime 컴포넌트가 없습니다.", obj);
            return;
        }

        slime.Initialize(_splineContainer, slimeData, health);
    }

    private void OnGameOver()
    {
        _gameOver = true;
        _spawnCts?.Cancel();
    }
}
