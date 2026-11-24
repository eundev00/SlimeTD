using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[PopupPresenter(typeof(GameResultPresenter))]
public class GameResultPopup : PopupBase
{
    [NotNull][SerializeField] private Button _restartButton;
    [NotNull][SerializeField] private Button _lobbyButton;
    [NotNull][SerializeField] private TMP_Text _energyCostText;

    [Header("결과 정보")]
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _waveText;
    [SerializeField] private GameObject _newRecordMark;
    [SerializeField] private GameObject _rewardRoot;
    [SerializeField] private RewardItemView _rewardItemTemplate;
    [SerializeField] private Sprite _coinIcon;
    [SerializeField] private Sprite _diamondIcon;
    [SerializeField] private Sprite _packTicketIcon;

    private readonly List<RewardItemView> _rewardItems = new List<RewardItemView>();

    public event Action RestartButtonClicked;
    public event Action LobbyButtonClicked;

    public string SceneName => gameObject.scene.name;

    private void Start()
    {
        if (_restartButton == null || _lobbyButton == null)
        {
            Debug.Log("[GameResultPopup] 버튼이 연결되지 않았습니다.", this);
            return;
        }

        _restartButton.onClick.AddListener(OnRestartButtonClicked);
        _lobbyButton.onClick.AddListener(OnLobbyButtonClicked);
    }

    private void OnDestroy()
    {
        if (_restartButton != null)
            _restartButton.onClick.RemoveListener(OnRestartButtonClicked);

        if (_lobbyButton != null)
            _lobbyButton.onClick.RemoveListener(OnLobbyButtonClicked);
    }

    private void OnRestartButtonClicked()
    {
        RestartButtonClicked?.Invoke();
    }

    private void OnLobbyButtonClicked()
    {
        LobbyButtonClicked?.Invoke();
    }

    protected override void OnBackPressed()
    {
        if (_lobbyButton == null || !_lobbyButton.interactable)
            return;

        OnLobbyButtonClicked();
    }

    public void Setup(bool isCleared, int reachedWave, bool isNewRecord)
    {
        if (_titleText != null)
            _titleText.text = isCleared ? "스테이지 클리어!" : "게임 오버";

        if (_waveText != null)
            _waveText.text = reachedWave.ToString();

        if (_newRecordMark != null)
            _newRecordMark.SetActive(isNewRecord);

        SetRewards(null);
        SetButtonsInteractable(true);
    }

    public void SetRewards(IReadOnlyList<(string codeName, long amount)> rewards)
    {
        if (_rewardRoot == null || _rewardItemTemplate == null)
            return;

        _rewardItemTemplate.gameObject.SetActive(false);
        foreach (var item in _rewardItems)
            Destroy(item.gameObject);
        _rewardItems.Clear();

        _rewardRoot.SetActive(rewards != null && rewards.Count > 0);
        if (rewards == null)
            return;

        foreach (var (codeName, amount) in rewards)
        {
            var item = Instantiate(_rewardItemTemplate, _rewardItemTemplate.transform.parent);
            item.gameObject.SetActive(true);
            item.Setup(GetIcon(codeName), amount);
            _rewardItems.Add(item);
        }

        // 칸이 넘치면 가운데가 아니라 첫 보상부터 보이게 한다
        var content = (RectTransform)_rewardItemTemplate.transform.parent;
        var scroll = content.GetComponentInParent<ScrollRect>();
        if (scroll != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            scroll.horizontalNormalizedPosition = 0f;
        }
    }

    private Sprite GetIcon(string codeName)
    {
        switch (codeName)
        {
            case ItemCatalog.Coin: return _coinIcon;
            case ItemCatalog.Diamond: return _diamondIcon;
            case ItemCatalog.PackTicket: return _packTicketIcon;
            default:
                Debug.Log($"[GameResultPopup] 아이콘이 없는 보상입니다: {codeName}", this);
                return null;
        }
    }

    public void SetEnergyCost(int cost)
    {
        if (_energyCostText != null)
            _energyCostText.text = cost.ToString();
    }

    public void SetButtonsInteractable(bool interactable)
    {
        if (_restartButton != null)
            _restartButton.interactable = interactable;

        if (_lobbyButton != null)
            _lobbyButton.interactable = interactable;
    }
}
