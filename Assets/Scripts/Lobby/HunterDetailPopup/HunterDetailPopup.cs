using System;
using MessagePipe;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

[PopupPresenter(typeof(HunterDetailPresenter))]
public class HunterDetailPopup : PopupBase
{
    [Header("재화 칩 (오른쪽 상단)")]
    [NotNull][SerializeField] private CurrencyView _cardsCurrency;
    [NotNull][SerializeField] private CurrencyView _goldCurrency;
    [NotNull][SerializeField] private CurrencyView _gemCurrency;

    [Header("헌터 정보")]
    [NotNull][SerializeField] private Image _thumbnail;
    [NotNull][SerializeField] private GradeBadge _gradeBadge;
    [NotNull][SerializeField] private TMP_Text _nameText;
    [NotNull][SerializeField] private TMP_Text _gradeTagText;

    [Header("레벨 줄")]
    [NotNull][SerializeField] private GameObject _levelRow;
    [NotNull][SerializeField] private TMP_Text _levelText;
    [NotNull][SerializeField] private RectTransform _progressFill;
    [NotNull][SerializeField] private TMP_Text _progressText;
    [NotNull][SerializeField] private GameObject _arrowBadge;
    [NotNull][SerializeField] private GameObject _lockedRow;

    [Header("스탯")]
    [NotNull][SerializeField] private TMP_Text _attackValueText;
    [NotNull][SerializeField] private TMP_Text _attackBonusText;
    [NotNull][SerializeField] private TMP_Text _speedValueText;

    [Header("버튼")]
    [NotNull][SerializeField] private Button _upgradeButton;
    [NotNull][SerializeField] private TMP_Text _upgradeCostText;
    [NotNull][SerializeField] private GameObject _upgradeDisabled;
    [NotNull][SerializeField] private GameObject _upgradeMax;
    [NotNull][SerializeField] private Button _buyButton;
    [NotNull][SerializeField] private TMP_Text _buyCostText;
    [NotNull][SerializeField] private Button _closeButton;

    [SerializeField] private Color _lockedThumbnailColor = new Color(0.11f, 0.12f, 0.11f);

    private readonly ReactiveProperty<int> _towerId = new ReactiveProperty<int>();

    public IReadOnlyReactiveProperty<int> TowerId => _towerId;

    public event Action UpgradeClicked;
    public event Action BuyClicked;
    public event Action CloseClicked;

    // 같은 헌터를 다시 열어도 값이 같아 알림이 생략되지 않도록 강제로 알린다.
    public void SetTowerId(int towerId)
    {
        _towerId.SetValueAndForceNotify(towerId);
    }

    [Inject]
    public void Construct(IPlayerDataService playerDataService, IPublisher<CurrencyViewClickedEvent> clickedPublisher)
    {
        _cardsCurrency.Initialize(playerDataService, clickedPublisher);
        _goldCurrency.Initialize(playerDataService, clickedPublisher);
        _gemCurrency.Initialize(playerDataService, clickedPublisher);
    }

    private void Start()
    {
        _upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
        _buyButton.onClick.AddListener(OnBuyButtonClicked);
        _closeButton.onClick.AddListener(OnCloseButtonClicked);
    }

    private void OnDestroy()
    {
        if (_upgradeButton != null)
            _upgradeButton.onClick.RemoveListener(OnUpgradeButtonClicked);

        if (_buyButton != null)
            _buyButton.onClick.RemoveListener(OnBuyButtonClicked);

        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(OnCloseButtonClicked);

        _towerId.Dispose();
    }

    protected override void OnClose()
    {
        _towerId.Value = 0;
    }

    public void SetHunterInfo(string name, int gradeId, string gradeName)
    {
        _nameText.text = name;
        _gradeBadge.Show(gradeId, gradeName);
        _gradeTagText.text = gradeName;
    }

    public void SetThumbnail(Sprite thumbnail, bool unlocked)
    {
        _thumbnail.sprite = thumbnail;
        _thumbnail.enabled = thumbnail != null;
        _thumbnail.color = unlocked ? Color.white : _lockedThumbnailColor;
    }

    public void SetUnlocked(bool unlocked)
    {
        _cardsCurrency.gameObject.SetActive(unlocked);
        _goldCurrency.gameObject.SetActive(unlocked);
        _gemCurrency.gameObject.SetActive(!unlocked);

        _levelRow.SetActive(unlocked);
        _lockedRow.SetActive(!unlocked);
    }

    public void SetLevel(int level, int maxLevel, bool canLevelUp)
    {
        _levelText.text = $"Lv.{level}/{maxLevel}";
        _arrowBadge.SetActive(canLevelUp);
    }

    public void SetCardProgress(int owned, int needed)
    {
        float ratio = needed > 0 ? Mathf.Clamp01((float)owned / needed) : 1f;
        _progressFill.anchorMax = new Vector2(ratio, 1f);
        _progressText.text = $"{owned}/{needed}";
    }

    public void SetCardProgressMax()
    {
        _progressFill.anchorMax = new Vector2(1f, 1f);
        _progressText.text = "MAX";
    }

    public void SetStats(string attack, string attackBonus, string attackSpeed)
    {
        bool hasBonus = !string.IsNullOrEmpty(attackBonus);

        _attackValueText.text = attack;
        _attackBonusText.gameObject.SetActive(hasBonus);
        _attackBonusText.text = hasBonus ? $"({attackBonus})" : string.Empty;
        _speedValueText.text = attackSpeed;
    }

    public void ShowBuy(string costText, bool enabled)
    {
        SetActionVisible(buy: true, upgrade: false);

        _buyButton.interactable = enabled;
        _buyCostText.text = costText;
    }

    public void ShowUpgrade(string costText, bool enabled)
    {
        SetActionVisible(buy: false, upgrade: true);

        _upgradeButton.interactable = enabled;
        _upgradeDisabled.SetActive(!enabled);
        _upgradeMax.SetActive(false);
        _upgradeCostText.text = costText;
    }

    public void ShowMaxLevel()
    {
        SetActionVisible(buy: false, upgrade: true);

        _upgradeButton.interactable = false;
        _upgradeDisabled.SetActive(true);
        _upgradeMax.SetActive(true);
        _upgradeCostText.text = string.Empty;
    }

    public void ShowUnavailable()
    {
        SetActionVisible(buy: false, upgrade: false);
    }

    private void SetActionVisible(bool buy, bool upgrade)
    {
        _buyButton.gameObject.SetActive(buy);
        _upgradeButton.gameObject.SetActive(upgrade);
    }

    private void OnUpgradeButtonClicked()
    {
        UpgradeClicked?.Invoke();
    }

    private void OnBuyButtonClicked()
    {
        BuyClicked?.Invoke();
    }

    private void OnCloseButtonClicked()
    {
        CloseClicked?.Invoke();
    }
}
