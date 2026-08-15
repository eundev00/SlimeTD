using Cysharp.Threading.Tasks;
using VContainer.Unity;

public class BootstrapEntryPoint : IStartable
{
    private readonly ISceneLoader _sceneLoader;

    public BootstrapEntryPoint(ISceneLoader sceneLoader)
    {
        _sceneLoader = sceneLoader;
    }

    public void Start()
    {
        _sceneLoader.LoadAdditiveAsync(SceneNames.Lobby).Forget();
    }
}
