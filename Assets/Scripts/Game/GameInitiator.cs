using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public class GameInitiator : IAsyncStartable
{
    private readonly IGameplayService _gameplayService;
    private readonly Zone _zone;

    public GameInitiator(IGameplayService gameplayService, Zone zone)
    {
        _gameplayService = gameplayService;
        _zone = zone;
    }

    public async UniTask StartAsync(CancellationToken ct)
    {
        await _gameplayService.InitializeAsync();
        _zone.Initialize(_gameplayService.TowerConfig, _gameplayService.WaveTable);
    }
}
