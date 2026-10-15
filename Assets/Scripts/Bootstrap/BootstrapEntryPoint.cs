using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public class BootstrapEntryPoint : IAsyncStartable
{
    private readonly ISceneLoader _sceneLoader;
    private readonly IPlayerDataService _playerDataService;

    public BootstrapEntryPoint(ISceneLoader sceneLoader, IPlayerDataService playerDataService)
    {
        _sceneLoader = sceneLoader;
        _playerDataService = playerDataService;
    }

    public async UniTask StartAsync(CancellationToken ct)
    {
        await _playerDataService.InitializeAsync();
        await _sceneLoader.LoadAdditiveAsync(SceneNames.Lobby);
    }
}