using Cysharp.Threading.Tasks;
using Services.UpdateService;
using TMPro;
using UnityEngine;

// 다음 에너지 1 회복까지 남은 시간. 서버 시계 기준으로 센다
public class EnergyTimerView : MonoBehaviour, IPeriodicUpdatable
{
    [NotNull][SerializeField] private GameObject _root;
    [NotNull][SerializeField] private TMP_Text _text;

    private IAccountService _accountService;
    private IUpdateSubscriptionService _updateService;
    private const float RefreshRetrySeconds = 3f;

    private bool _refreshing;
    private float _nextRefreshAt;

    public void Initialize(IAccountService accountService, IUpdateSubscriptionService updateService)
    {
        _accountService = accountService;
        _updateService = updateService;
        _updateService.RegisterPeriodicUpdatable(this, 0.25f);
        Refresh();
    }

    private void OnDestroy()
    {
        _updateService?.UnregisterPeriodicUpdatable(this);
    }

    public void ManagedPeriodicUpdate(float deltaTime)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (_root == null || _text == null || _accountService == null)
            return;

        long recoverAt = _accountService.EnergyRecoverAtMs.Value;
        if (recoverAt <= 0)
        {
            _root.SetActive(false);
            return;
        }

        long remainMs = recoverAt - _accountService.ServerNowMs;
        if (remainMs <= 0)
        {
            // 회복 시각이 지나면 서버에서 에너지와 다음 회복 시각을 다시 받는다
            _text.text = "00:00";
            RequestRefreshAsync();
            return;
        }

        _root.SetActive(true);
        long seconds = (remainMs + 999) / 1000;
        _text.text = $"{seconds / 60:00}:{seconds % 60:00}";
    }

    // 시계 차이(최대 1초)로 서버가 아직 회복 전 값을 줄 수 있어 너무 자주 묻지 않는다
    private void RequestRefreshAsync()
    {
        if (_refreshing || Time.realtimeSinceStartup < _nextRefreshAt)
            return;

        _nextRefreshAt = Time.realtimeSinceStartup + RefreshRetrySeconds;
        RefreshAsync().Forget();
    }

    private async UniTaskVoid RefreshAsync()
    {
        _refreshing = true;
        await _accountService.RefreshItemsAsync();
        _refreshing = false;
    }
}
