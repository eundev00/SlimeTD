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
    [NotNull][SerializeField] private WaveTableData _waveTable;
    [NotNull][SerializeField] private SplineContainer _splineContainer;
    [SerializeField] private bool _autoStart = true;

    private IGameObjectPoolService _poolService;
    private IPublisher<GameProgressEvent> _gameProgressPublisher;
    private ISubscriber<GameProgressEvent> _gameProgressSubscriber;

    private CompositeDisposable _disposables;
    private CancellationTokenSource _spawnCts;
    private bool _waveClearedReceived;

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

    private void Start()
    {
        if (_splineContainer == null)
        {
            Debug.Log("[WaveSpawner] _splineContainer가 연결되지 않았습니다.", this);
            return;
        }

        if (_waveTable == null || _waveTable.Waves == null || _waveTable.Waves.Length == 0)
        {
            Debug.Log("[WaveSpawner] _waveTable이 비어 있습니다.", this);
            return;
        }

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

    private void PreparePools()
    {
        // 전체 웨이브에서 각 프리팹별로 총 몇 개가 필요한지 카운트
        var prefabCounts = new Dictionary<GameObject, int>();

        foreach (var wave in _waveTable.Waves)
        {
            if (wave?.SpawnEntries == null)
                continue;

            foreach (var entry in wave.SpawnEntries)
            {
                if (entry?.SlimePrefabs == null)
                    continue;

                foreach (var prefab in entry.SlimePrefabs)
                {
                    if (prefab == null)
                        continue;

                    if (!prefabCounts.ContainsKey(prefab))
                        prefabCounts[prefab] = 0;

                    prefabCounts[prefab]++;
                }
            }
        }

        // 카운트 기반으로 풀 생성
        foreach (var kvp in prefabCounts)
        {
            int totalCount = kvp.Value;
            int initialSize = Mathf.Max(totalCount, 10); // 최소 10개
            int maxSize = Mathf.CeilToInt(totalCount * 2f); // 여유분 2배

            _poolService.CreatePool(kvp.Key, initialSize, maxSize);
            Debug.Log($"[WaveSpawner] 풀 생성: {kvp.Key.name}, 초기={initialSize}, 최대={maxSize}");
        }
    }

    private async UniTaskVoid RunAllWavesAsync()
    {
        var token = _spawnCts.Token;

        try
        {
            foreach (var wave in _waveTable.Waves)
            {
                if (wave == null)
                    continue;

                await RunWaveAsync(wave, token);
            }
        }
        catch (System.OperationCanceledException)
        {
            // 게임오버 또는 파괴로 스폰 루프 취소 시 정상 종료.
        }
    }

    private async UniTask RunWaveAsync(WaveData wave, System.Threading.CancellationToken token)
    {
        // 총 슬라임 개수 계산
        int totalSlimeCount = CalculateTotalSlimeCount(wave);

        _waveClearedReceived = false;
        _gameProgressPublisher.Publish(new GameProgressEvent(GameProgressType.WaveStarted, wave.WaveIndex, totalSlimeCount));
        Debug.Log($"[WaveSpawner] 웨이브 {wave.WaveIndex} 시작");

        if (wave.StartDelay > 0f)
            await UniTask.Delay(System.TimeSpan.FromSeconds(wave.StartDelay), cancellationToken: token);

        // 스폰 실행
        if (wave.SpawnEntries != null)
        {
            foreach (var entry in wave.SpawnEntries)
            {
                if (entry?.SlimePrefabs == null)
                    continue;

                foreach (var prefab in entry.SlimePrefabs)
                {
                    if (prefab == null)
                        continue;

                    SpawnOne(prefab);

                    if (entry.SpawnInterval > 0f)
                        await UniTask.Delay(System.TimeSpan.FromSeconds(entry.SpawnInterval), cancellationToken: token);
                }
            }
        }

        _gameProgressPublisher.Publish(new GameProgressEvent(GameProgressType.WaveSpawnFinished, wave.WaveIndex));
        Debug.Log($"[WaveSpawner] 웨이브 {wave.WaveIndex} 스폰 완료");

        // WaveCleared 대기
        await UniTask.WaitUntil(() => _waveClearedReceived, cancellationToken: token);
    }

    private int CalculateTotalSlimeCount(WaveData wave)
    {
        int count = 0;
        if (wave.SpawnEntries != null)
        {
            foreach (var entry in wave.SpawnEntries)
            {
                if (entry?.SlimePrefabs != null)
                    count += entry.SlimePrefabs.Length;
            }
        }
        return count;
    }

    private void SpawnOne(GameObject prefab)
    {
        var obj = _poolService.Get(prefab);
        if (obj == null)
            return;

        obj.transform.position = _splineContainer.EvaluatePosition(0f);

        var slime = obj.GetComponent<BaseSlime>();
        if (slime == null)
        {
            Debug.Log("[WaveSpawner] 슬라임 프리팹에 BaseSlime 컴포넌트가 없습니다.", obj);
            return;
        }

        slime.Initialize(_splineContainer);
    }

    private void OnGameOver()
    {
        _spawnCts?.Cancel();
    }
}
