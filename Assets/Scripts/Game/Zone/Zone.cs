using MessagePipe;
using UniRx;
using UnityEngine;
using VContainer;

public class Zone : MonoBehaviour
{
    [NotNull][SerializeField] private GridMapReference _gridMapReference;
    [NotNull][SerializeField] private WaveSpawner _waveSpawner;
    [NotNull][SerializeField] private TowerSpawner _towerSpawner;
    [NotNull][SerializeField] private TowerInputHandler _towerInputHandler;
    [NotNull][SerializeField] private Transform _spawnRoot;

    private IObjectResolver _resolver;
    private IGroundHeightSampler _groundHeightSampler;
    private ISubscriber<TowerSpawnRequestedEvent> _spawnRequestedSubscriber;

    private TowerCells _towerCells;
    private CompositeDisposable _disposables;

    [Inject]
    public void Construct(
        IObjectResolver resolver,
        IGroundHeightSampler groundHeightSampler,
        ISubscriber<TowerSpawnRequestedEvent> spawnRequestedSubscriber)
    {
        _resolver = resolver;
        _groundHeightSampler = groundHeightSampler;
        _spawnRequestedSubscriber = spawnRequestedSubscriber;
    }

    public void Initialize(WaveTableData waveTable)
    {
        if (_gridMapReference == null || _gridMapReference.GridMapData == null)
        {
            Debug.Log("[Zone] GridMapReference가 연결되지 않았습니다.", this);
            return;
        }

        _towerCells = new TowerCells(_gridMapReference.GridMapData);

        if (_towerSpawner != null)
        {
            _resolver.Inject(_towerSpawner);
            _towerSpawner.Initialize(_towerCells, _spawnRoot);
        }

        if (_towerInputHandler != null)
        {
            _towerInputHandler.Initialize(_towerCells, _groundHeightSampler);
        }

        if (_waveSpawner != null)
        {
            _resolver.Inject(_waveSpawner);
            _waveSpawner.Initialize(waveTable, _spawnRoot);
        }

        _disposables?.Dispose();
        _disposables = new CompositeDisposable();
        _spawnRequestedSubscriber.Subscribe(_ => OnTowerSpawnRequested()).AddTo(_disposables);
    }

    // MessagePipe 브로커는 ProjectLifetimeScope 소속이라 씬을 넘어 산다. 여기서 안 끊으면 죽은 Zone이 계속 받는다.
    private void OnDestroy()
    {
        _disposables?.Dispose();
        _disposables = null;
    }

    private void OnTowerSpawnRequested()
    {
        if (_towerSpawner == null)
            return;

        _towerSpawner.TrySpawnRandom();
    }
}
