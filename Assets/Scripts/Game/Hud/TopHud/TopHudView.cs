using System;
using UnityEngine;
using UnityEngine.UI;

public class TopHudView : MonoBehaviour
{
    [NotNull][SerializeField] private Button _lobbyButton;

    public event Action LobbyButtonClicked;

    public string SceneName => gameObject.scene.name;

    private void Start()
    {
        if (_lobbyButton == null)
        {
            Debug.Log("[TopHudView] _lobbyButton이 연결되지 않았습니다.", this);
            return;
        }

        _lobbyButton.onClick.AddListener(() => LobbyButtonClicked?.Invoke());
    }

    public void SetLobbyButtonInteractable(bool interactable)
    {
        if (_lobbyButton != null)
            _lobbyButton.interactable = interactable;
    }
}
