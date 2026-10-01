using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[PopupPresenter(typeof(GameResultPresenter))]
public class GameResultPopup : PopupBase
{
    [NotNull][SerializeField] private Button _restartButton;
    [NotNull][SerializeField] private Button _lobbyButton;

    [Header("결과 정보")]
    [SerializeField] private TMP_Text _waveText;
    [SerializeField] private GameObject _rewardRoot;

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

    public void Setup(bool isCleared, int currentWave)
    {
        // TODO: isCleared 사용 예정 (클리어/게임오버 구분 연출)
        if (_waveText != null)
            _waveText.text = currentWave.ToString();

        if (_rewardRoot != null)
            _rewardRoot.SetActive(true);

        SetButtonsInteractable(true);
    }

    public void SetButtonsInteractable(bool interactable)
    {
        if (_restartButton != null)
            _restartButton.interactable = interactable;

        if (_lobbyButton != null)
            _lobbyButton.interactable = interactable;
    }
}
