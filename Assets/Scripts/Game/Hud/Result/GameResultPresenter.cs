using System;
using Cysharp.Threading.Tasks;
using MessagePipe;
using Services.PopupService;
using UniRx;
using VContainer.Unity;

public class GameResultPresenter : IStartable, IDisposable
{
    private readonly IPopupService _popupService;
    private readonly ISceneLoader _sceneLoader;
    private readonly IGameplayService _gameplayService;
    private readonly ISubscriber<GameProgressEvent> _gameProgressSubscriber;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private GameResultPopup _popup;
    private bool _shown;
    private bool _disposed;

    public GameResultPresenter(
        IPopupService popupService,
        ISceneLoader sceneLoader,
        IGameplayService gameplayService,
        ISubscriber<GameProgressEvent> gameProgressSubscriber)
    {
        _popupService = popupService;
        _sceneLoader = sceneLoader;
        _gameplayService = gameplayService;
        _gameProgressSubscriber = gameProgressSubscriber;
    }

    public void Start()
    {
        _gameProgressSubscriber.Subscribe(HandleGameProgress).AddTo(_disposables);
    }

    private void HandleGameProgress(GameProgressEvent e)
    {
        if (_shown)
            return;

        if (e.EventType != GameProgressType.GameOver && e.EventType != GameProgressType.StageCleared)
            return;

        _shown = true;

        bool isCleared = e.EventType == GameProgressType.StageCleared;
        ShowAsync(isCleared).Forget();
    }

    private async UniTaskVoid ShowAsync(bool isCleared)
    {
        var popup = await _popupService.OpenAsync<GameResultPopup>(PrefabKeys.GameResultPopup);
        if (popup == null || _disposed)
            return;

        _popup = popup;
        _popup.RestartButtonClicked += HandleRestartButtonClicked;
        _popup.LobbyButtonClicked += HandleLobbyButtonClicked;

        var info = _gameplayService.Info;
        _popup.Setup(isCleared, info.CurrentWave.Value, info.MaxWave.Value);
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
        _disposed = true;

        if (_popup != null)
        {
            _popup.RestartButtonClicked -= HandleRestartButtonClicked;
            _popup.LobbyButtonClicked -= HandleLobbyButtonClicked;
        }

        _disposables.Dispose();
    }
}
