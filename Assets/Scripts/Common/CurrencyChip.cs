using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class CurrencyChip : MonoBehaviour
{
    [SerializeField] private CurrencyType _type;
    [NotNull][SerializeField] private TMP_Text _text;
    [NotNull][SerializeField] private Button _button;

    private IPlayerDataService _playerDataService;
    private IDisposable _subscription;

    public CurrencyType Type => _type;
    public bool Enabled { get; set; } = true;

    public event Action<CurrencyType> Clicked;

    [Inject]
    public void Construct(IPlayerDataService playerDataService)
    {
        _playerDataService = playerDataService;

        // 주입이 OnEnable보다 늦게 올 수 있다
        if (isActiveAndEnabled)
            Subscribe();
    }

    private void Start()
    {
        if (_button == null)
        {
            Debug.Log("[CurrencyChip] 버튼이 연결되지 않았다", this);
            return;
        }

        _button.onClick.AddListener(OnButtonClicked);
    }

    private void OnEnable()
    {
        if (_playerDataService != null)
            Subscribe();
    }

    private void OnDisable()
    {
        _subscription?.Dispose();
        _subscription = null;
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnButtonClicked);
    }

    private void Subscribe()
    {
        _subscription?.Dispose();
        _subscription = GetSource().Subscribe(SetValue);
    }

    private IReadOnlyReactiveProperty<int> GetSource()
    {
        switch (_type)
        {
            case CurrencyType.Gold: return _playerDataService.Gold;
            case CurrencyType.Gem: return _playerDataService.Gem;
            default: return _playerDataService.Cards;
        }
    }

    private void SetValue(int value)
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
