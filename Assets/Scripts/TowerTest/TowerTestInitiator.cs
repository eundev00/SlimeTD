using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public class TowerTestInitiator : IAsyncStartable
{
    private readonly IGameplayService _gameplayService;
    private readonly TowerTestSpawner _spawner;

    public TowerTestInitiator(IGameplayService gameplayService, TowerTestSpawner spawner)
    {
        _gameplayService = gameplayService;
        _spawner = spawner;
    }

    public async UniTask StartAsync(CancellationToken ct)
    {
        await _gameplayService.InitializeAsync();
        _spawner.Initialize(_gameplayService.Catalog);
    }
}
