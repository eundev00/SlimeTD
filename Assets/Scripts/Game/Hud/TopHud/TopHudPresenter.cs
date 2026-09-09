using System;
using Cysharp.Threading.Tasks;
using UniRx;
using VContainer.Unity;

public class TopHudPresenter : IStartable, IDisposable
{
    private readonly TopHudView _view;
    private readonly ISceneLoader _sceneLoader;
    private readonly IGameplayService _gameplayService;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    public TopHudPresenter(TopHudView view, ISceneLoader sceneLoader, IGameplayService gameplayService)
    {
        _view = view;
        _sceneLoader = sceneLoader;
        _gameplayService = gameplayService;
    }

    public void Start()
    {
        _view.LobbyButtonClicked += HandleLobbyButtonClicked;

        var info = _gameplayService.Info;

        info.CurrentWave
            .CombineLatest(info.MaxWave, (current, max) => (current, max))
            .Subscribe(x => _view.SetWave(x.current, x.max))
            .AddTo(_disposables);

        info.Life
            .Subscribe(_view.SetLife)
            .AddTo(_disposables);
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

        _disposables.Dispose();
    }
}
