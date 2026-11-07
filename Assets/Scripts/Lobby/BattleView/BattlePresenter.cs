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

    // TODO: 헌터 카드 뽑기 팝업 열기
    private void HandleHunterCardGachaClicked()
    {
        UnityEngine.Debug.Log("[BattlePresenter] 헌터 카드 뽑기 버튼");
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
