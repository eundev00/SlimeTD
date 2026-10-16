using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

[PopupPresenter(typeof(HunterDetailPresenter))]
public class HunterDetailPopup : PopupBase
{
    [Header("재화 칩 (오른쪽 상단)")]
    [NotNull][SerializeField] private GameObject _cardsChip;
    [NotNull][SerializeField] private TMP_Text _cardsChipText;
    [NotNull][SerializeField] private GameObject _goldChip;
    [NotNull][SerializeField] private TMP_Text _goldChipText;
    [NotNull][SerializeField] private GameObject _gemChip;
    [NotNull][SerializeField] private TMP_Text _gemChipText;

    [Header("헌터 정보")]
    [NotNull][SerializeField] private Image _heroImage;
    [NotNull][SerializeField] private TMP_Text _heroGradeBadgeText;
    [NotNull][SerializeField] private TMP_Text _nameText;
    [NotNull][SerializeField] private Image _gradeTagBackground;
    [NotNull][SerializeField] private TMP_Text _gradeTagText;
    [NotNull][SerializeField] private TMP_Text _attackTypeTagText;

    [Header("레벨 줄")]
    [NotNull][SerializeField] private GameObject _levelRow;
    [NotNull][SerializeField] private TMP_Text _levelText;
    [NotNull][SerializeField] private RectTransform _progressFill;
    [NotNull][SerializeField] private TMP_Text _progressText;
    [NotNull][SerializeField] private GameObject _arrowBadge;
    [NotNull][SerializeField] private GameObject _lockedRow;

    [Header("스탯")]
    [NotNull][SerializeField] private Image _attackTileBorder;
    [NotNull][SerializeField] private TMP_Text _attackValueText;
    [NotNull][SerializeField] private GameObject _attackBonusBadge;
    [NotNull][SerializeField] private TMP_Text _attackBonusText;
    [NotNull][SerializeField] private TMP_Text _speedValueText;

    [Header("버튼")]
    [NotNull][SerializeField] private Button _actionButton;
    [NotNull][SerializeField] private Image _actionBackground;
    [NotNull][SerializeField] private TMP_Text _actionLabel;
    [NotNull][SerializeField] private TMP_Text _actionCostText;
    [NotNull][SerializeField] private Image _actionCostIcon;
    [NotNull][SerializeField] private TMP_Text _lockedNoticeText;
    [NotNull][SerializeField] private Button _closeButton;

    [Header("색")]
    [SerializeField] private Color _levelUpColor = new Color(0.91f, 0.75f, 0.29f);
    [SerializeField] private Color _unlockColor = new Color(0.43f, 0.56f, 0.84f);
    [SerializeField] private Color _disabledColor = new Color(0.45f, 0.45f, 0.40f);
    [SerializeField] private Color _goldIconColor = new Color(0.16f, 0.18f, 0.17f);
    [SerializeField] private Color _gemIconColor = Color.white;
    [SerializeField] private Color _costNormalColor = new Color(0.16f, 0.18f, 0.17f);
    [SerializeField] private Color _costInsufficientColor = new Color(0.78f, 0.18f, 0.18f);
    [SerializeField] private Color _tileActiveColor = new Color(0.25f, 0.66f, 0.35f);
    [SerializeField] private Color _tileNormalColor = new Color(0.37f, 0.40f, 0.37f);
    [SerializeField] private Color _heroNormalColor = new Color(0.55f, 0.59f, 0.55f);
    [SerializeField] private Color _heroLockedColor = new Color(0.11f, 0.12f, 0.11f);

    [SerializeField]
    private Color[] _gradeColors =
    {
        new Color(0.75f, 0.72f, 0.65f),
        new Color(0.43f, 0.56f, 0.84f),
        new Color(0.60f, 0.45f, 0.81f),
        new Color(0.88f, 0.71f, 0.29f),
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
        _cardsChip.SetActive(data.Unlocked);
        _goldChip.SetActive(data.Unlocked);
        _gemChip.SetActive(!data.Unlocked);

        _cardsChipText.text = data.Cards.ToString("N0");
        _goldChipText.text = data.Gold.ToString("N0");
        _gemChipText.text = data.Gem.ToString("N0");
    }

    private void RenderInfo(HunterDetailViewData data)
    {
        _nameText.text = data.Name;
        _heroGradeBadgeText.text = data.GradeId.ToString();
        _heroImage.color = data.Unlocked ? _heroNormalColor : _heroLockedColor;

        int index = Mathf.Clamp(data.GradeId - 1, 0, _gradeColors.Length - 1);
        _gradeTagBackground.color = _gradeColors[index];
        _gradeTagText.text = data.GradeName;
        _attackTypeTagText.text = data.AttackTypeName;
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

    private void RenderStats(HunterDetailViewData data)
    {
        _attackValueText.text = data.AttackValue;
        _speedValueText.text = data.AttackSpeed;

        bool hasBonus = !string.IsNullOrEmpty(data.AttackBonus);
        _attackBonusBadge.SetActive(hasBonus);
        _attackBonusText.text = data.AttackBonus;
        _attackTileBorder.color = data.CanLevelUp ? _tileActiveColor : _tileNormalColor;
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

        _actionButton.interactable = data.ActionEnabled;
        _actionLabel.text = isMax ? "MAX" : (isUnlock ? "해금" : "레벨업");
        _actionLabel.color = isUnlock ? Color.white : _costNormalColor;

        _actionBackground.color = !data.ActionEnabled && !isUnlock
            ? _disabledColor
            : (isUnlock ? _unlockColor : _levelUpColor);

        _actionCostText.gameObject.SetActive(!isMax);
        _actionCostIcon.gameObject.SetActive(!isMax);
        _actionCostText.text = data.CostText;
        _actionCostIcon.color = isUnlock ? _gemIconColor : _goldIconColor;

        Color costColor = isUnlock ? Color.white : _costNormalColor;
        _actionCostText.color = data.CostInsufficient ? _costInsufficientColor : costColor;
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