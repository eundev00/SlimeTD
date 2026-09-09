using System;
using UnityEngine;
using UnityEngine.UI;

public class GameResultView : MonoBehaviour
{
    [NotNull][SerializeField] private GameObject _panelRoot;
    [NotNull][SerializeField] private Button _restartButton;
    [NotNull][SerializeField] private Button _lobbyButton;

    public event Action RestartButtonClicked;
    public event Action LobbyButtonClicked;

    public string SceneName => gameObject.scene.name;

    private void Awake()
    {
        Hide();
    }

    private void Start()
    {
        if (_panelRoot == null)
        {
            Debug.Log("[GameResultView] _panelRoot가 연결되지 않았습니다.", this);
            return;
        }

        if (_restartButton == null || _lobbyButton == null)
        {
            Debug.Log("[GameResultView] 버튼이 연결되지 않았습니다.", this);
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

    public void Show()
    {
        SetButtonsInteractable(true);

        if (_panelRoot != null)
            _panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (_panelRoot != null)
            _panelRoot.SetActive(false);
    }

    public void SetButtonsInteractable(bool interactable)
    {
        if (_restartButton != null)
            _restartButton.interactable = interactable;

        if (_lobbyButton != null)
            _lobbyButton.interactable = interactable;
    }
}
