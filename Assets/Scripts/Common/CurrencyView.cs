using System;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrencyView : MonoBehaviour
{
    [SerializeField] private CurrencyType _type;
    [NotNull][SerializeField] private TMP_Text _text;
    [NotNull][SerializeField] private Button _button;

    private CurrencyViewPresenter _presenter;

    public CurrencyType Type => _type;

    public event Action Clicked;

    public void Initialize(IPlayerDataService playerDataService, IPublisher<CurrencyViewClickedEvent> clickedPublisher)
    {
        _presenter?.Dispose();
        _presenter = new CurrencyViewPresenter(this, playerDataService, clickedPublisher);
    }

    private void Start()
    {
        if (_button == null)
        {
            Debug.Log("[CurrencyView] 버튼이 연결되지 않았다", this);
            return;
        }

        _button.onClick.AddListener(OnButtonClicked);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnButtonClicked);

        _presenter?.Dispose();
    }

    public void SetValue(int value)
    {
        _text.text = value.ToString("N0");
    }

    private void OnButtonClicked()
    {
        Clicked?.Invoke();
    }
}
