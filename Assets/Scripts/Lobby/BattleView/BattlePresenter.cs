using System;
using Cysharp.Threading.Tasks;
using SlimeTD.Protocol;
using VContainer.Unity;

public class BattlePresenter : IStartable, IDisposable
{
    private readonly BattleView _view;
    private readonly ISceneLoader _sceneLoader;
    private readonly IPopupService _popupService;
    private readonly ISingleBattleService _singleBattleService;
    private readonly StageData _stage;

    public BattlePresenter(BattleView view, ISceneLoader sceneLoader, IPopupService popupService, ISingleBattleService singleBattleService, StageData stage)
    {
        _view = view;
        _sceneLoader = sceneLoader;
        _popupService = popupService;
        _singleBattleService = singleBattleService;
        _stage = stage;
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
        StartSingleAsync().Forget();
    }

    // 어떤 스테이지·맵을 띄울지는 서버 응답으로 정하므로 응답을 받은 뒤에 씬을 바꾼다
    private async UniTaskVoid StartSingleAsync()
    {
        _view.SetStartInteractable(false);

        var result = await _singleBattleService.StartAsync(_stage.StageId);
        if (result != ResultCode.Ok)
        {
            // TODO: 실패 안내 UI
            _view.SetStartInteractable(true);
            return;
        }

        _sceneLoader.TransitionAsync(SceneNames.Lobby, SceneNames.SingleGame).Forget();
    }

    private void HandleDevTestClicked()
    {
        _view.SetStartInteractable(false);
        _sceneLoader.TransitionAsync(SceneNames.Lobby, SceneNames.MultiGame).Forget();
    }
}
