using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HunterCardGachaPackView : MonoBehaviour
{
    [SerializeField] private int _packId;
    [NotNull][SerializeField] private Button _button;
    [NotNull][SerializeField] private GameObject _buyEnabled;
    [NotNull][SerializeField] private GameObject _buyDisabled;
    [NotNull][SerializeField] private TMP_Text _enabledPriceText;
    [NotNull][SerializeField] private TMP_Text _disabledPriceText;

    public int PackId => _packId;

    public event Action<int> Clicked;

    private void Start()
    {
        if (_button == null)
        {
            Debug.Log("[HunterCardGachaPackView] 버튼이 연결되지 않았습니다.", this);
            return;
        }

        _button.onClick.AddListener(OnClicked);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnClicked);
    }

    public void SetPrice(int price)
    {
        string text = price.ToString("N0");
        _enabledPriceText.text = text;
        _disabledPriceText.text = text;
    }

    public void SetAffordable(bool affordable)
    {
        _button.interactable = affordable;
        _buyEnabled.SetActive(affordable);
        _buyDisabled.SetActive(!affordable);
    }

    private void OnClicked()
    {
        Clicked?.Invoke(_packId);
    }
}
