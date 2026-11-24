using Cysharp.Threading.Tasks;
using SlimeTD.Protocol;
using UniRx;
using UnityEngine;

public class HunterCardGachaPresenter : IPopupPresenter
{
    private readonly HunterCardGachaPopup _popup;
    private readonly IHunterCardGachaService _gachaService;
    private readonly IAccountService _accountService;
    private readonly IPopupService _popupService;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private HunterCardGachaConfig _config;
    private bool _drawing;
    private bool _disposed;

    public HunterCardGachaPresenter(HunterCardGachaPopup popup, IHunterCardGachaService gachaService, IAccountService accountService, IPopupService popupService)
    {
        _popup = popup;
        _gachaService = gachaService;
        _accountService = accountService;
        _popupService = popupService;
    }

    public void Initialize()
    {
        _popup.PackClicked += HandlePackClicked;
        _popup.CloseClicked += HandleCloseClicked;

        // 설정을 읽기 전에는 가격을 모르므로 일단 모두 비활성으로 둔다
        foreach (var pack in _popup.Packs)
            pack.SetAffordable(false);

        LoadAsync().Forget();
    }

    public void Dispose()
    {
        _disposed = true;
        _popup.PackClicked -= HandlePackClicked;
        _popup.CloseClicked -= HandleCloseClicked;
        _disposables.Dispose();
    }

    private async UniTaskVoid LoadAsync()
    {
        var config = await _gachaService.GetConfigAsync();
        if (_disposed || config == null)
            return;

        _config = config;
        foreach (var pack in _popup.Packs)
        {
            if (config.TryGetPack(pack.PackId, out var data))
                pack.SetPrice(data.Price);
            else
                Debug.Log($"[HunterCardGachaPresenter] 설정에 없는 팩입니다: {pack.PackId}", pack);
        }

        _accountService.PackTicket.Subscribe(_ => RenderAffordable()).AddTo(_disposables);
    }

    private void RenderAffordable()
    {
        if (_config == null)
            return;

        int tickets = _accountService.PackTicket.Value;
        foreach (var pack in _popup.Packs)
        {
            bool affordable = !_drawing && _config.TryGetPack(pack.PackId, out var data) && tickets >= data.Price;
            pack.SetAffordable(affordable);
        }
    }

    private void HandlePackClicked(int packId)
    {
        if (_drawing)
            return;

        DrawAsync(packId).Forget();
    }

    // 연타로 두 번 사지 않도록 결과가 나올 때까지 버튼을 잠근다
    private async UniTaskVoid DrawAsync(int packId)
    {
        _drawing = true;
        _popup.ShowMessage(string.Empty);
        RenderAffordable();

        var (code, result) = await _gachaService.DrawAsync(packId);

        _drawing = false;
        if (_disposed)
            return;

        RenderAffordable();

        if (result == null)
        {
            Debug.Log($"[HunterCardGachaPresenter] 뽑기에 실패했습니다: {packId} {code}");
            _popup.ShowMessage(FailMessage(code));
            return;
        }

        var revealPopup = await _popupService.OpenAsync<HunterCardRevealPopup>(PrefabKeys.HunterCardRevealPopup);
        if (revealPopup != null)
            revealPopup.SetResult(result);
    }

    private static string FailMessage(ResultCode code)
    {
        return code switch
        {
            ResultCode.NotEnoughItems => "팩 티켓이 부족합니다",
            ResultCode.NotFound => "없는 상품입니다",
            ResultCode.NotAvailable => "뽑을 수 있는 헌터가 없습니다",
            _ => "서버 요청에 실패했습니다",
        };
    }

    private void HandleCloseClicked()
    {
        _popup.RequestClose();
    }
}
