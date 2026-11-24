using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SlimeTD.Protocol;
using UniRx;
using UnityEngine;

public class GameResultPresenter : IPopupPresenter
{
    private readonly GameResultPopup _popup;
    private readonly ISceneLoader _sceneLoader;
    private readonly IGameplayService _gameplayService;
    private readonly IAccountService _accountService;
    private readonly ISingleBattleService _singleBattleService;
    private readonly IResourceLoadService _resourceLoadService;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private ItemCatalog _itemCatalog;

    public GameResultPresenter(GameResultPopup popup, ISceneLoader sceneLoader, IGameplayService gameplayService, IAccountService accountService,
        ISingleBattleService singleBattleService, IResourceLoadService resourceLoadService)
    {
        _popup = popup;
        _sceneLoader = sceneLoader;
        _gameplayService = gameplayService;
        _accountService = accountService;
        _singleBattleService = singleBattleService;
        _resourceLoadService = resourceLoadService;
    }

    public void Initialize()
    {
        var info = _gameplayService.Info;

        // 로비를 거치지 않은 에디터 테스트에서는 기록이 저장되지 않아 신기록을 표시하지 않는다
        bool isNewRecord = _accountService.IsInitialized && info.ReachedWave.Value > info.PreviousBestWave.Value;

        _popup.Setup(info.IsCleared.Value, info.ReachedWave.Value, isNewRecord);
        _popup.SetEnergyCost(EnergyCost);

        _popup.RestartButtonClicked += HandleRestartButtonClicked;
        _popup.LobbyButtonClicked += HandleLobbyButtonClicked;

        SubscribeRewardsAsync().Forget();
    }

    private async UniTaskVoid SubscribeRewardsAsync()
    {
        _itemCatalog = await _resourceLoadService.LoadAsync<ItemCatalog>(DataKeys.ItemCatalog);
        if (_disposables.IsDisposed)
            return;

        // 서버 결과 응답은 팝업이 열린 뒤에 온다
        _singleBattleService.Rewards
            .Subscribe(ShowRewards)
            .AddTo(_disposables);
    }

    private void ShowRewards(IReadOnlyList<ItemStack> rewards)
    {
        if (rewards == null || _itemCatalog == null)
        {
            _popup.SetRewards(null);
            return;
        }

        var list = new List<(string, long)>(rewards.Count);
        foreach (var reward in rewards)
            list.Add((_itemCatalog.GetCodeName(reward.ItemId), reward.Amount));

        _popup.SetRewards(list);
    }

    private void HandleRestartButtonClicked()
    {
        if (!IsServerBattle)
        {
            _popup.SetButtonsInteractable(false);
            _sceneLoader.ReloadAsync(_popup.SceneName).Forget();
            return;
        }

        if (_accountService.Energy.Value < EnergyCost)
        {
            ShowNotEnoughEnergy();
            return;
        }

        RestartSingleAsync().Forget();
    }

    private bool IsServerBattle => _popup.SceneName == SceneNames.SingleGame && _accountService.IsInitialized;

    private int EnergyCost => _gameplayService.Config != null ? _gameplayService.Config.EnergyCostPerBattle : 0;

    // 다시 하기도 새 전투라 서버에 시작을 받은 뒤에 씬을 다시 연다
    private async UniTaskVoid RestartSingleAsync()
    {
        _popup.SetButtonsInteractable(false);

        var result = await _singleBattleService.StartAsync(_gameplayService.Stage.StageId);
        if (result != ResultCode.Ok)
        {
            if (result == ResultCode.NotEnoughEnergy)
                ShowNotEnoughEnergy();

            // TODO: 실패 안내 UI
            _popup.SetButtonsInteractable(true);
            return;
        }

        _sceneLoader.ReloadAsync(_popup.SceneName).Forget();
    }

    private void ShowNotEnoughEnergy()
    {
        // TODO: 광고 보기 / 다이아 구매 팝업
        Debug.Log($"[GameResultPresenter] 에너지가 부족합니다: {_accountService.Energy.Value}/{EnergyCost}");
    }

    private void HandleLobbyButtonClicked()
    {
        _popup.SetButtonsInteractable(false);
        _sceneLoader.TransitionAsync(_popup.SceneName, SceneNames.Lobby).Forget();
    }

    public void Dispose()
    {
        _disposables.Dispose();
        _popup.RestartButtonClicked -= HandleRestartButtonClicked;
        _popup.LobbyButtonClicked -= HandleLobbyButtonClicked;
    }
}
