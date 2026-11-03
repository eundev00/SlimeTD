using System;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public class BattlePresenter : IStartable, IDisposable
{
    private readonly BattleView _view;
    private readonly ISceneLoader _sceneLoader;

    public BattlePresenter(BattleView view, ISceneLoader sceneLoader)
    {
        _view = view;
        _sceneLoader = sceneLoader;
    }

    public void Start()
    {
        _view.StartClicked += HandleStartClicked;
        _view.DevTestClicked += HandleDevTestClicked;
    }

    public void Dispose()
    {
        if (_view == null)
            return;

        _view.StartClicked -= HandleStartClicked;
        _view.DevTestClicked -= HandleDevTestClicked;
    }

    private void HandleStartClicked()
    {
        _view.SetStartInteractable(false);
        _sceneLoader.TransitionAsync(SceneNames.Lobby, SceneNames.Game).Forget();
    }

    private void HandleDevTestClicked()
    {
        _view.SetStartInteractable(false);
        _sceneLoader.TransitionAsync(SceneNames.Lobby, SceneNames.Game3).Forget();
    }
}
