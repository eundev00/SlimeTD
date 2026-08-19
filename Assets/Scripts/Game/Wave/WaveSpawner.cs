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
    private IPublisher<WaveStartedEvent> _waveStartedPublisher;
    private IPublisher<WaveClearedEvent> _waveClearedPublisher;
    private ISubscriber<SlimeKilledEvent> _killedSubscriber;
    private ISubscriber<SlimeReachedEndEvent> _reachedEndSubscriber;
    private ISubscriber<GameOverEvent> _gameOverSubscriber;

    private CompositeDisposable _disposables;
    private CancellationTokenSource _spawnCts;
    private int _aliveCount;
    private bool _spawnFinished;
    private int _currentWaveIndex;

    [Inject]
    public void Construct(
        IGameObjectPoolService poolService,
        IPublisher<WaveStartedEvent> waveStartedPublisher,
        IPublisher<WaveClearedEvent> waveClearedPublisher,
        ISubscriber<SlimeKilledEvent> killedSubscriber,
        ISubscriber<SlimeReachedEndEvent> reachedEndSubscriber,
        ISubscriber<GameOverEvent> gameOverSubscriber)
    {
        _poolService = poolService;
        _waveStartedPublisher = waveStartedPublisher;
        _waveClearedPublisher = waveClearedPublisher;
        _killedSubscriber = killedSubscriber;
        _reachedEndSubscriber = reachedEndSubscriber;
        _gameOverSubscriber = gameOverSubscriber;
    }

    #region Unity Lifecycle

    private void Start()
    {
        if (_splineContainer == null)
        {
            Debug.LogError("[WaveSpawner] _splineContainer가 연결되지 않았습니다.", this);
            return;
        }

        if (_waveTable == null || _waveTable.Waves == null || _waveTable.Waves.Length == 0)
        {
            Debug.LogError("[WaveSpawner] _waveTable이 비어 있습니다.", this);
            return;
        }

        _disposables = new CompositeDisposable();
        _killedSubscriber.Subscribe(_ => OnSlimeRemoved()).AddTo(_disposables);
        _reachedEndSubscriber.Subscribe(_ => OnSlimeRemoved()).AddTo(_disposables);
        _gameOverSubscriber.Subscribe(_ => OnGameOver()).AddTo(_disposables);

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

    #endregion

    private void PreparePools()
    {
        var prepared = new HashSet<GameObject>();
        foreach (var wave in _waveTable.Waves)
        {
            if (wave?.SpawnEntries == null)
                continue;

            foreach (var entry in wave.SpawnEntries)
            {
                if (entry?.SlimePrefab == null || !prepared.Add(entry.SlimePrefab))
                    continue;

                _poolService.CreatePool(entry.SlimePrefab, 10, 50);
            }
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
        _currentWaveIndex = wave.WaveIndex;
        _aliveCount = 0;
        _spawnFinished = false;

        _waveStartedPublisher.Publish(new WaveStartedEvent(wave.WaveIndex));
        Debug.Log($"[WaveSpawner] 웨이브 {wave.WaveIndex} 시작");

        if (wave.StartDelay > 0f)
            await UniTask.Delay(System.TimeSpan.FromSeconds(wave.StartDelay), cancellationToken: token);

        if (wave.SpawnEntries != null)
        {
            foreach (var entry in wave.SpawnEntries)
            {
                if (entry?.SlimePrefab == null)
                    continue;

                for (int i = 0; i < entry.Count; i++)
                {
                    SpawnOne(entry.SlimePrefab);

                    if (entry.SpawnInterval > 0f)
                        await UniTask.Delay(System.TimeSpan.FromSeconds(entry.SpawnInterval), cancellationToken: token);
                }
            }
        }

        _spawnFinished = true;

        // 마지막 스폰 전에 앞선 슬라임이 모두 처치되면 WaitUntil이 영영 안 깨어나므로 여기서 먼저 판정한다.
        if (_aliveCount <= 0)
        {
            PublishCleared();
            return;
        }

        await UniTask.WaitUntil(() => _aliveCount <= 0, cancellationToken: token);
        PublishCleared();
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
            Debug.LogError("[WaveSpawner] 슬라임 프리팹에 BaseSlime 컴포넌트가 없습니다.", obj);
            return;
        }

        slime.Initialize(_splineContainer);
        _aliveCount++;
    }

    private void OnSlimeRemoved()
    {
        _aliveCount = Mathf.Max(0, _aliveCount - 1);
    }

    private void OnGameOver()
    {
        _spawnCts?.Cancel();
    }

    private void PublishCleared()
    {
        _waveClearedPublisher.Publish(new WaveClearedEvent(_currentWaveIndex));
        Debug.Log($"[WaveSpawner] 웨이브 {_currentWaveIndex} 클리어");
    }
}
