using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

[PopupPresenter(typeof(HunterDetailPresenter))]
public class HunterDetailPopup : PopupBase
{
    [Header("재화 칩 (오른쪽 상단)")]
    [NotNull][SerializeField] private CurrencyChip _cardsChip;
    [NotNull][SerializeField] private CurrencyChip _goldChip;
    [NotNull][SerializeField] private CurrencyChip _gemChip;

    [Header("헌터 정보")]
    [NotNull][SerializeField] private Image _heroImage;
    [NotNull][SerializeField] private TMP_Text _heroGradeBadgeText;
    [NotNull][SerializeField] private TMP_Text _nameText;
    [NotNull][SerializeField] private Image _gradeTagBackground;
    [NotNull][SerializeField] private Image _gradeTagBorder;
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
    [NotNull][SerializeField] private TMP_Text _speedValueText;

    [Header("버튼")]
    [NotNull][SerializeField] private Button _actionButton;
    [NotNull][SerializeField] private Image _actionBackground;
    [NotNull][SerializeField] private TMP_Text _actionLabel;
    [NotNull][SerializeField] private TMP_Text _actionCostText;
    [NotNull][SerializeField] private Image _actionCostIcon;
    [NotNull][SerializeField] private TMP_Text _lockedNoticeText;
    [NotNull][SerializeField] private Button _closeButton;

    [Header("비용 아이콘")]
    [SerializeField] private Sprite _goldCostSprite;
    [SerializeField] private Sprite _gemCostSprite;

    [Header("색")]
    [SerializeField] private Color _levelUpColor = new Color(0.91f, 0.75f, 0.29f);
    [SerializeField] private Color _unlockColor = new Color(0.43f, 0.56f, 0.84f);
    [SerializeField] private Color _disabledColor = new Color(0.55f, 0.44f, 0.12f);
    [SerializeField] private Color _goldIconColor = Color.white;
    [SerializeField] private Color _gemIconColor = Color.white;
    [SerializeField] private Color _iconDisabledColor = new Color(0.6f, 0.6f, 0.6f);
    [SerializeField] private Color _labelNormalColor = new Color(0.35f, 0.24f, 0.17f);
    [SerializeField] private Color _labelDisabledColor = new Color(0.33f, 0.25f, 0.07f);
    [SerializeField] private Color _costNormalColor = Color.white;
    [SerializeField] private Color _costDisabledColor = new Color(0.62f, 0.62f, 0.62f);
    [SerializeField] private Color _costInsufficientColor = new Color(0.95f, 0.32f, 0.32f);
    [SerializeField] private Color _bonusColor = new Color(0.18f, 0.62f, 0.34f);
    [SerializeField] private Color _heroNormalColor = Color.white;
    [SerializeField] private Color _heroLockedColor = new Color(0.11f, 0.12f, 0.11f);

    // 인덱스는 gradeId - 1: 일반 희귀 영웅 전설. 배지(채움) 색과 프레임(테두리) 색이다.
    [SerializeField]
    private Color[] _gradeColors =
    {
        new Color32(0xD5, 0xA7, 0x7C, 0xFF),
        new Color32(0x75, 0xC5, 0xFF, 0xFF),
        new Color32(0xC5, 0x9A, 0xF2, 0xFF),
        new Color32(0xFF, 0xD5, 0x6A, 0xFF),
    };

    [SerializeField]
    private Color[] _gradeBorderColors =
    {
        new Color32(0xA8, 0x79, 0x52, 0xFF),
        new Color32(0x25, 0x88, 0xE8, 0xFF),
        new Color32(0x9B, 0x58, 0xD6, 0xFF),
        new Color32(0xE7, 0xA5, 0x2E, 0xFF),
    };

    private readonly ReactiveProperty<int> _towerId = new ReactiveProperty<int>();

    public IReadOnlyReactiveProperty<int> TowerId => _towerId;

    public event Action ActionClicked;
    public event Action CloseClicked;

    // 같은 헌터를 다시 열어도 값이 같아 알림이 생략되지 않도록 강제로 알린다.
    public void SetTowerId(int towerId)
    {
        _towerId.SetValueAndForceNotify(towerId);
    }

    private void Start()
    {
        _actionButton.onClick.AddListener(OnActionButtonClicked);
        _closeButton.onClick.AddListener(OnCloseButtonClicked);
    }

    private void OnDestroy()
    {
        if (_actionButton != null)
            _actionButton.onClick.RemoveListener(OnActionButtonClicked);

        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(OnCloseButtonClicked);

        _towerId.Dispose();
    }

    protected override void OnClose()
    {
        _towerId.Value = 0;
    }

    public void Render(HunterDetailViewData data)
    {
        RenderChips(data);
        RenderInfo(data);
        RenderLevelRow(data);
        RenderStats(data);
        RenderAction(data);
    }

    private void RenderChips(HunterDetailViewData data)
    {
        _cardsChip.gameObject.SetActive(data.Unlocked);
        _goldChip.gameObject.SetActive(data.Unlocked);
        _gemChip.gameObject.SetActive(!data.Unlocked);
    }

    private void RenderInfo(HunterDetailViewData data)
    {
        _nameText.text = data.Name;
        _heroGradeBadgeText.text = data.GradeId.ToString();
        _heroImage.color = data.Unlocked ? _heroNormalColor : _heroLockedColor;

        int index = Mathf.Clamp(data.GradeId - 1, 0, _gradeColors.Length - 1);
        _gradeTagBackground.color = _gradeColors[index];
        _gradeTagBorder.color = _gradeBorderColors[Mathf.Clamp(index, 0, _gradeBorderColors.Length - 1)];
        _gradeTagText.text = data.GradeName;
    }

    private void RenderLevelRow(HunterDetailViewData data)
    {
        _levelRow.SetActive(data.Unlocked);
        _lockedRow.SetActive(!data.Unlocked);

        if (!data.Unlocked)
            return;

        _levelText.text = $"Lv.{data.Level}/{data.MaxLevel}";
        _arrowBadge.SetActive(data.CanLevelUp);

        if (data.Mode == HunterDetailActionMode.MaxLevel)
        {
            _progressFill.anchorMax = new Vector2(1f, 1f);
            _progressText.text = "MAX";
            return;
        }

        float ratio = data.NeededCards > 0 ? Mathf.Clamp01((float)data.OwnedCards / data.NeededCards) : 1f;
        _progressFill.anchorMax = new Vector2(ratio, 1f);
        _progressText.text = $"{data.OwnedCards}/{data.NeededCards}";
    }

    // 레벨업으로 늘어나는 양은 박스 색이 아니라 값 옆의 초록 글씨로 보여 준다.
    private void RenderStats(HunterDetailViewData data)
    {
        string bonus = string.IsNullOrEmpty(data.AttackBonus)
            ? string.Empty
            : $" <size=70%><color=#{ColorUtility.ToHtmlStringRGB(_bonusColor)}>{data.AttackBonus}</color></size>";

        _attackValueText.text = data.AttackValue + bonus;
        _speedValueText.text = data.AttackSpeed;
    }

    private void RenderAction(HunterDetailViewData data)
    {
        bool unavailable = data.Mode == HunterDetailActionMode.UnlockUnavailable;
        _actionButton.gameObject.SetActive(!unavailable);
        _lockedNoticeText.gameObject.SetActive(unavailable);

        if (unavailable)
            return;

        bool isUnlock = data.Mode == HunterDetailActionMode.Unlock;
        bool isMax = data.Mode == HunterDetailActionMode.MaxLevel;

        bool dimmed = !data.ActionEnabled && !isUnlock;

        _actionButton.interactable = data.ActionEnabled;
        _actionLabel.text = isMax ? "MAX" : (isUnlock ? "해금" : "레벨업");
        _actionLabel.color = isUnlock ? Color.white : (dimmed ? _labelDisabledColor : _labelNormalColor);

        _actionBackground.color = dimmed
            ? _disabledColor
            : (isUnlock ? _unlockColor : _levelUpColor);

        _actionCostText.gameObject.SetActive(!isMax);
        _actionCostIcon.gameObject.SetActive(!isMax);
        _actionCostText.text = data.CostText;
        _actionCostIcon.color = dimmed ? _iconDisabledColor : (isUnlock ? _gemIconColor : _goldIconColor);

        var costSprite = isUnlock ? _gemCostSprite : _goldCostSprite;
        if (costSprite != null)
            _actionCostIcon.sprite = costSprite;

        _actionCostText.color = data.CostInsufficient
            ? _costInsufficientColor
            : (dimmed ? _costDisabledColor : _costNormalColor);
    }

    private void OnActionButtonClicked()
    {
        ActionClicked?.Invoke();
    }

    private void OnCloseButtonClicked()
    {
        CloseClicked?.Invoke();
    }
}
