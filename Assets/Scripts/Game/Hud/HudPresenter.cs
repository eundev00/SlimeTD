using System;
using Cysharp.Threading.Tasks;

public class HudViewPresenter : IDisposable
{
    private readonly HudView _view;
    private readonly ISceneLoader _sceneLoader;

    public HudViewPresenter(HudView view, ISceneLoader sceneLoader)
    {
        _view = view;
        _sceneLoader = sceneLoader;

        _view.LobbyButtonClicked += HandleLobbyButtonClicked;
    }

    private void HandleLobbyButtonClicked()
    {
        _view.SetLobbyButtonInteractable(false);
        _sceneLoader.TransitionAsync(SceneNames.Game, SceneNames.Lobby).Forget();
    }

    public void Dispose()
    {
        if (_view != null)
            _view.LobbyButtonClicked -= HandleLobbyButtonClicked;
    }
}
