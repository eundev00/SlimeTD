using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public class TowerTestInitiator : IAsyncStartable
{
    private readonly IGameplayService _gameplayService;
    private readonly ITowerPrefabProvider _prefabProvider;
    private readonly TowerTestSpawner _spawner;

    public TowerTestInitiator(IGameplayService gameplayService, ITowerPrefabProvider prefabProvider, TowerTestSpawner spawner)
    {
        _gameplayService = gameplayService;
        _prefabProvider = prefabProvider;
        _spawner = spawner;
    }

    public async UniTask StartAsync(CancellationToken ct)
    {
        await _gameplayService.InitializeAsync();
        await _prefabProvider.LoadAsync(_gameplayService.Catalog);
        _spawner.Initialize(_gameplayService.Catalog);
    }
}
