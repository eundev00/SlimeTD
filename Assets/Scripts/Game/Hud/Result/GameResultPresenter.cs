using Cysharp.Threading.Tasks;

public class GameResultPresenter : IPopupPresenter
{
    private readonly GameResultPopup _popup;
    private readonly ISceneLoader _sceneLoader;
    private readonly IGameplayService _gameplayService;

    public GameResultPresenter(GameResultPopup popup, ISceneLoader sceneLoader, IGameplayService gameplayService)
    {
        _popup = popup;
        _sceneLoader = sceneLoader;
        _gameplayService = gameplayService;
    }

    public void Initialize()
    {
        var info = _gameplayService.Info;
        _popup.Setup(info.IsCleared.Value, info.CurrentWave.Value);

        _popup.RestartButtonClicked += HandleRestartButtonClicked;
        _popup.LobbyButtonClicked += HandleLobbyButtonClicked;
    }

    private void HandleRestartButtonClicked()
    {
        _popup.SetButtonsInteractable(false);
        _sceneLoader.ReloadAsync(_popup.SceneName).Forget();
    }

    private void HandleLobbyButtonClicked()
    {
        _popup.SetButtonsInteractable(false);
        _sceneLoader.TransitionAsync(_popup.SceneName, SceneNames.Lobby).Forget();
    }

    public void Dispose()
    {
        _popup.RestartButtonClicked -= HandleRestartButtonClicked;
        _popup.LobbyButtonClicked -= HandleLobbyButtonClicked;
    }
}
