using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public class GameInitiator : IAsyncStartable, IDisposable
{
    private readonly IGameplayService _gameplayService;
    private readonly ITowerLevelService _towerLevelService;
    private readonly Zone _zone;
    private readonly SlimeDepthBucketBinder _depthBucketBinder = new SlimeDepthBucketBinder();

    public GameInitiator(IGameplayService gameplayService, ITowerLevelService towerLevelService, Zone zone)
    {
        _gameplayService = gameplayService;
        _towerLevelService = towerLevelService;
        _zone = zone;
    }

    public async UniTask StartAsync(CancellationToken ct)
    {
        await _gameplayService.InitializeAsync();

        // 로비에서 레벨을 정하는 구조가 생기기 전까지 디버그 레벨을 기본값으로 쓴다.
        if (_gameplayService.Config != null)
            _towerLevelService.SetDefaultLevel(_gameplayService.Config.DebugTowerLevel);

        _depthBucketBinder.Bind(_gameplayService.DepthBucketSettings);
        _zone.Initialize(_gameplayService.WaveTable);
    }

    public void Dispose()
    {
        _depthBucketBinder.Dispose();
    }
}
