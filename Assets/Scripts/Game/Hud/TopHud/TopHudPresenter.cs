using System;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public class TopHudPresenter : IStartable, IDisposable
{
    private readonly TopHudView _view;
    private readonly ISceneLoader _sceneLoader;

    public TopHudPresenter(TopHudView view, ISceneLoader sceneLoader)
    {
        _view = view;
        _sceneLoader = sceneLoader;
    }

    public void Start()
    {
        _view.LobbyButtonClicked += HandleLobbyButtonClicked;
    }

    private void HandleLobbyButtonClicked()
    {
        _view.SetLobbyButtonInteractable(false);
        _sceneLoader.TransitionAsync(_view.SceneName, SceneNames.Lobby).Forget();
    }

    public void Dispose()
    {
        if (_view != null)
            _view.LobbyButtonClicked -= HandleLobbyButtonClicked;
    }
}
