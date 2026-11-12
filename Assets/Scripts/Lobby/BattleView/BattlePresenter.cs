using System;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public class BattlePresenter : IStartable, IDisposable
{
    private readonly BattleView _view;
    private readonly ISceneLoader _sceneLoader;
    private readonly IPopupService _popupService;

    public BattlePresenter(BattleView view, ISceneLoader sceneLoader, IPopupService popupService)
    {
        _view = view;
        _sceneLoader = sceneLoader;
        _popupService = popupService;
    }

    public void Start()
    {
        _view.StartClicked += HandleStartClicked;
        _view.DevTestClicked += HandleDevTestClicked;
        _view.HunterCardGachaClicked += HandleHunterCardGachaClicked;
        _view.SetHunterCardGachaNotice(false);
    }

    public void Dispose()
    {
        if (_view == null)
            return;

        _view.StartClicked -= HandleStartClicked;
        _view.DevTestClicked -= HandleDevTestClicked;
        _view.HunterCardGachaClicked -= HandleHunterCardGachaClicked;
    }

    private void HandleHunterCardGachaClicked()
    {
        _popupService.OpenAsync<HunterCardGachaPopup>(PrefabKeys.HunterCardGachaPopup).Forget();
    }

    private void HandleStartClicked()
    {
        _view.SetStartInteractable(false);
        _sceneLoader.TransitionAsync(SceneNames.Lobby, SceneNames.SingleGame).Forget();
    }

    private void HandleDevTestClicked()
    {
        _view.SetStartInteractable(false);
        _sceneLoader.TransitionAsync(SceneNames.Lobby, SceneNames.SingleGame).Forget();
    }
}
