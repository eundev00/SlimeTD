using System;
using Cysharp.Threading.Tasks;
using SlimeTD.Protocol;
using UnityEngine;
using VContainer.Unity;

public class BattlePresenter : IStartable, IDisposable
{
    private readonly BattleView _view;
    private readonly ISceneLoader _sceneLoader;
    private readonly IPopupService _popupService;
    private readonly ISingleBattleService _singleBattleService;
    private readonly IAccountService _accountService;
    private readonly IResourceLoadService _resourceLoadService;
    private readonly StageData _stage;

    private int _energyCost;

    public BattlePresenter(BattleView view, ISceneLoader sceneLoader, IPopupService popupService, ISingleBattleService singleBattleService,
        IAccountService accountService, IResourceLoadService resourceLoadService, StageData stage)
    {
        _view = view;
        _sceneLoader = sceneLoader;
        _popupService = popupService;
        _singleBattleService = singleBattleService;
        _accountService = accountService;
        _resourceLoadService = resourceLoadService;
        _stage = stage;
    }

    public void Start()
    {
        _view.StartClicked += HandleStartClicked;
        _view.DevTestClicked += HandleDevTestClicked;
        _view.HunterCardGachaClicked += HandleHunterCardGachaClicked;
        _view.SetHunterCardGachaNotice(false);
        LoadEnergyCostAsync().Forget();
    }

    public void Dispose()
    {
        if (_view == null)
            return;

        _view.StartClicked -= HandleStartClicked;
        _view.DevTestClicked -= HandleDevTestClicked;
        _view.HunterCardGachaClicked -= HandleHunterCardGachaClicked;
    }

    private async UniTaskVoid LoadEnergyCostAsync()
    {
        var config = await _resourceLoadService.LoadAsync<GameConfig>(DataKeys.GameConfig);
        if (config == null || _view == null)
            return;

        _energyCost = config.EnergyCostPerBattle;
        _view.SetEnergyCost(_energyCost);
    }

    private void HandleHunterCardGachaClicked()
    {
        _popupService.OpenAsync<HunterCardGachaPopup>(PrefabKeys.HunterCardGachaPopup).Forget();
    }

    private void HandleStartClicked()
    {
        if (_accountService.Energy.Value < _energyCost)
        {
            ShowNotEnoughEnergy();
            return;
        }

        StartSingleAsync().Forget();
    }

    // 어떤 스테이지·맵을 띄울지는 서버 응답으로 정하므로 응답을 받은 뒤에 씬을 바꾼다
    private async UniTaskVoid StartSingleAsync()
    {
        _view.SetStartInteractable(false);

        var result = await _singleBattleService.StartAsync(_stage.StageId);
        if (result != ResultCode.Ok)
        {
            if (result == ResultCode.NotEnoughEnergy)
                ShowNotEnoughEnergy();

            // TODO: 실패 안내 UI
            _view.SetStartInteractable(true);
            return;
        }

        _sceneLoader.TransitionAsync(SceneNames.Lobby, SceneNames.SingleGame).Forget();
    }

    private void ShowNotEnoughEnergy()
    {
        // TODO: 광고 보기 / 다이아 구매 팝업
        Debug.Log($"[BattlePresenter] 에너지가 부족합니다: {_accountService.Energy.Value}/{_energyCost}");
    }

    private void HandleDevTestClicked()
    {
        _view.SetStartInteractable(false);
        _sceneLoader.TransitionAsync(SceneNames.Lobby, SceneNames.MultiGame).Forget();
    }
}
