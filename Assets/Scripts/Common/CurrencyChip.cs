using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrencyChip : MonoBehaviour
{
    [SerializeField] private CurrencyType _type;
    [NotNull][SerializeField] private TMP_Text _text;
    [NotNull][SerializeField] private Button _button;

    public CurrencyType Type => _type;
    public bool Enabled { get; set; } = true;

    public event Action<CurrencyType> Clicked;

    private void Start()
    {
        if (_button == null)
        {
            Debug.Log("[CurrencyChip] 버튼이 연결되지 않았다", this);
            return;
        }

        _button.onClick.AddListener(OnButtonClicked);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnButtonClicked);
    }

    public void SetValue(int value)
    {
        _text.text = value.ToString("N0");
    }

    private void OnButtonClicked()
    {
        if (!Enabled)
            return;

        Clicked?.Invoke(_type);
    }
}
