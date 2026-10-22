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
    }

    public void Dispose()
    {
        if (_view != null)
            _view.StartClicked -= HandleStartClicked;
    }

    private void HandleStartClicked()
    {
        _view.SetStartInteractable(false);
        _sceneLoader.TransitionAsync(SceneNames.Lobby, SceneNames.Game).Forget();
    }
}
