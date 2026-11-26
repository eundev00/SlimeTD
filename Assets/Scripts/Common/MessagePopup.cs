using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 문구와 확인 버튼만 있는 공통 팝업. 확인 뒤 동작은 여는 쪽이 Confirmed로 정한다
public class MessagePopup : PopupBase
{
    [NotNull][SerializeField] private TMP_Text _titleText;
    [NotNull][SerializeField] private TMP_Text _messageText;
    [NotNull][SerializeField] private Button _confirmButton;

    public event Action Confirmed;

    private void Start()
    {
        if (_confirmButton == null)
        {
            Debug.Log("[MessagePopup] 확인 버튼이 연결되지 않았습니다.", this);
            return;
        }

        _confirmButton.onClick.AddListener(Confirm);
    }

    private void OnDestroy()
    {
        if (_confirmButton != null)
            _confirmButton.onClick.RemoveListener(Confirm);
    }

    public void Setup(string title, string message)
    {
        if (_titleText != null)
            _titleText.text = title;

        if (_messageText != null)
            _messageText.text = message;

        if (_confirmButton != null)
            _confirmButton.interactable = true;
    }

    protected override void OnBackPressed()
    {
        Confirm();
    }

    private void Confirm()
    {
        if (_confirmButton == null || !_confirmButton.interactable)
            return;

        _confirmButton.interactable = false;
        Confirmed?.Invoke();
    }
}
