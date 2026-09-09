using System;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UniRx;
using VContainer.Unity;

public class GameResultPresenter : IStartable, IDisposable
{
    private readonly GameResultView _view;
    private readonly ISceneLoader _sceneLoader;
    private readonly ISubscriber<GameProgressEvent> _gameProgressSubscriber;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private bool _shown;

    public GameResultPresenter(
        GameResultView view,
        ISceneLoader sceneLoader,
        ISubscriber<GameProgressEvent> gameProgressSubscriber)
    {
        _view = view;
        _sceneLoader = sceneLoader;
        _gameProgressSubscriber = gameProgressSubscriber;
    }

    public void Start()
    {
        _view.RestartButtonClicked += HandleRestartButtonClicked;
        _view.LobbyButtonClicked += HandleLobbyButtonClicked;

        _gameProgressSubscriber.Subscribe(HandleGameProgress).AddTo(_disposables);
    }

    private void HandleGameProgress(GameProgressEvent e)
    {
        if (_shown)
            return;

        if (e.EventType != GameProgressType.GameOver && e.EventType != GameProgressType.StageCleared)
            return;

        _shown = true;
        _view.Show();
    }

    private void HandleRestartButtonClicked()
    {
        _view.SetButtonsInteractable(false);
        _sceneLoader.ReloadAsync(_view.SceneName).Forget();
    }

    private void HandleLobbyButtonClicked()
    {
        _view.SetButtonsInteractable(false);
        _sceneLoader.TransitionAsync(_view.SceneName, SceneNames.Lobby).Forget();
    }

    public void Dispose()
    {
        if (_view != null)
        {
            _view.RestartButtonClicked -= HandleRestartButtonClicked;
            _view.LobbyButtonClicked -= HandleLobbyButtonClicked;
        }

        _disposables.Dispose();
    }
}
