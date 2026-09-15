using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public class GameInitiator : IAsyncStartable, IDisposable
{
    private readonly IGameplayService _gameplayService;
    private readonly Zone _zone;
    private readonly SlimeDepthBucketBinder _depthBucketBinder = new SlimeDepthBucketBinder();

    public GameInitiator(IGameplayService gameplayService, Zone zone)
    {
        _gameplayService = gameplayService;
        _zone = zone;
    }

    public async UniTask StartAsync(CancellationToken ct)
    {
        await _gameplayService.InitializeAsync();
        _depthBucketBinder.Bind(_gameplayService.DepthBucketSettings);
        _zone.Initialize(_gameplayService.WaveTable);
    }

    public void Dispose()
    {
        _depthBucketBinder.Dispose();
    }
}
