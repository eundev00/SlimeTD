using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public class BootstrapEntryPoint : IAsyncStartable
{
    private readonly ISceneLoader _sceneLoader;
    private readonly IAccountService _accountService;
    private readonly ServerConnectErrorView _errorView;

    public BootstrapEntryPoint(ISceneLoader sceneLoader, IAccountService accountService, ServerConnectErrorView errorView)
    {
        _sceneLoader = sceneLoader;
        _accountService = accountService;
        _errorView = errorView;
    }

    public async UniTask StartAsync(CancellationToken ct)
    {
        while (!await _accountService.InitializeAsync())
            await _errorView.WaitRetryAsync();

        await _sceneLoader.LoadAdditiveAsync(SceneNames.Lobby);
    }
}