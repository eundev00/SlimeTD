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
    // 웨이브가 겹쳐 스폰되므로 동시 생존 수를 이 창(window) 크기로 근사한다.
    [SerializeField] private int _overlapWaveWindow = 3;

    private StageData _stage;
    private Transform _spawnRoot;
    private SplineContainer _splineContainer;
    private IGameObjectPoolService _poolService;
    private IPublisher<GameProgressEvent> _gameProgressPublisher;
    private ISubscriber<GameProgressEvent> _gameProgressSubscriber;
    private IGameplayService _gameplayService;
    private ISpawnOrderCounter _spawnOrderCounter;
    private ISlimePrefabProvider _slimePrefabProvider;

    private CompositeDisposable _disposables;
    private CancellationTokenSource _spawnCts;
    private bool _waveClearedReceived;
    private bool _gameOver;

    [Inject]
    public void Construct(
        IGameObjectPoolService poolService,
        IPublisher<GameProgressEvent> gameProgressPublisher,
        ISubscriber<GameProgressEvent> gameProgressSubscriber,
        ISpawnOrderCounter spawnOrderCounter,
        IGameplayService gameplayService,
        ISlimePrefabProvider slimePrefabProvider)
    {
        _poolService = poolService;
        _gameProgressPublisher = gameProgressPublisher;
        _gameProgressSubscriber = gameProgressSubscriber;
        _spawnOrderCounter = spawnOrderCounter;
        _gameplayService = gameplayService;
        _slimePrefabProvider = slimePrefabProvider;
    }

    public void Initialize(StageData stage, Transform spawnRoot, SplineContainer path)
    {
        _stage = stage;
        _spawnRoot = spawnRoot;
        _splineContainer = path;

        if (_splineContainer == null)
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
        _waveClearedReceived = false;
        _spawnOrderCounter.Reset();

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

        if (_gameplayService.Config.AutoStartWave)
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

    private async UniTaskVoid RunAllWavesAsync()
    {
        var token = _spawnCts.Token;

        try
        {
            for (int waveIndex = 1; waveIndex <= _stage.FinalWave; waveIndex++)
            {
                await RunWaveAsync(waveIndex, token);
            }

            if (_gameOver)
                return;

            _gameProgressPublisher.Publish(new GameProgressEvent(GameProgressType.StageCleared, _stage.FinalWave));
            Debug.Log("[WaveSpawner] 스테이지 클리어");
        }
        catch (OperationCanceledException)
        {
            // 게임오버 또는 파괴로 스폰 루프 취소 시 정상 종료.
        }
    }

    private async UniTask RunWaveAsync(int waveIndex, CancellationToken token)
    {
        var plans = new List<SpawnPlan>();
        WaveResolver.Resolve(_stage, waveIndex, plans, true);

        if (plans.Count == 0)
        {
            Debug.Log($"[WaveSpawner] 웨이브 {waveIndex} 스폰 목록이 비어 건너뜁니다.", this);
            return;
        }

        float startDelay = _stage.InterWaveDelay;
        if (startDelay > 0f)
            await UniTask.Delay(TimeSpan.FromSeconds(startDelay), cancellationToken: token);

        bool isLastWave = waveIndex == _stage.FinalWave;
        int totalSlimeCount = plans.Count;

        _waveClearedReceived = false;
        _gameProgressPublisher.Publish(
            new GameProgressEvent(GameProgressType.WaveStarted, waveIndex, totalSlimeCount, isLastWave));
        Debug.Log($"[WaveSpawner] 웨이브 {waveIndex} 시작, 슬라임 {totalSlimeCount}마리 [{DescribePlans(plans)}]");

        foreach (var plan in plans)
        {
            if (plan.DelayBefore > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(plan.DelayBefore), cancellationToken: token);

            SpawnOne(plan);

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

    private void SpawnOne(SpawnPlan plan)
    {
        var slimeData = plan.SlimeData;

        if (!_slimePrefabProvider.TryGet(slimeData, out var prefab))
        {
            Debug.Log($"[WaveSpawner] 슬라임 프리팹이 없습니다: {slimeData.CodeName}", this);
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

        slime.SetDepthBucket(_spawnOrderCounter.NextBucket(slimeData.RenderGroup));

        slime.Initialize(_splineContainer, slimeData, plan.Health, plan.JellyReward);
    }

    private void OnGameOver()
    {
        _gameOver = true;
        _spawnCts?.Cancel();
    }
}
