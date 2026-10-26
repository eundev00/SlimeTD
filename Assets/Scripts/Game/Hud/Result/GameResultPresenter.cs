using Cysharp.Threading.Tasks;

public class GameResultPresenter : IPopupPresenter
{
    private readonly GameResultPopup _popup;
    private readonly ISceneLoader _sceneLoader;
    private readonly IGameplayService _gameplayService;
    private readonly IPlayerDataService _playerDataService;

    public GameResultPresenter(GameResultPopup popup, ISceneLoader sceneLoader, IGameplayService gameplayService, IPlayerDataService playerDataService)
    {
        _popup = popup;
        _sceneLoader = sceneLoader;
        _gameplayService = gameplayService;
        _playerDataService = playerDataService;
    }

    public void Initialize()
    {
        var info = _gameplayService.Info;

        // 로비를 거치지 않은 에디터 테스트에서는 기록이 저장되지 않아 신기록을 표시하지 않는다
        bool isNewRecord = _playerDataService.IsInitialized && info.ReachedWave.Value > info.PreviousBestWave.Value;

        _popup.Setup(info.IsCleared.Value, info.ReachedWave.Value, isNewRecord);

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
