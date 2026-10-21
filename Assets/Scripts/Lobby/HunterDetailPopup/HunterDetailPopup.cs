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
    [NotNull][SerializeField] private CurrencyChip _cardsChip;
    [NotNull][SerializeField] private CurrencyChip _goldChip;
    [NotNull][SerializeField] private CurrencyChip _gemChip;

    [Header("헌터 정보")]
    [NotNull][SerializeField] private TMP_Text _heroGradeBadgeText;
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
    [NotNull][SerializeField] private TMP_Text _speedValueText;

    [Header("버튼")]
    [NotNull][SerializeField] private Button _actionButton;
    [NotNull][SerializeField] private TMP_Text _actionLabel;
    [NotNull][SerializeField] private TMP_Text _actionCostText;
    [NotNull][SerializeField] private Image _actionCostIcon;
    [NotNull][SerializeField] private TMP_Text _lockedNoticeText;
    [NotNull][SerializeField] private Button _closeButton;

    [Header("비용 아이콘")]
    [SerializeField] private Sprite _goldCostSprite;
    [SerializeField] private Sprite _gemCostSprite;

    private readonly ReactiveProperty<int> _towerId = new ReactiveProperty<int>();

    public IReadOnlyReactiveProperty<int> TowerId => _towerId;

    public event Action ActionClicked;
    public event Action CloseClicked;

    // 같은 헌터를 다시 열어도 값이 같아 알림이 생략되지 않도록 강제로 알린다.
    public void SetTowerId(int towerId)
    {
        _towerId.SetValueAndForceNotify(towerId);
    }

    [Inject]
    public void Construct(IPlayerDataService playerDataService, IPublisher<CurrencyChipClickedEvent> clickedPublisher)
    {
        _cardsChip.Initialize(playerDataService, clickedPublisher);
        _goldChip.Initialize(playerDataService, clickedPublisher);
        _gemChip.Initialize(playerDataService, clickedPublisher);
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

    private void RenderStats(HunterDetailViewData data)
    {
        string bonus = string.IsNullOrEmpty(data.AttackBonus)
            ? string.Empty
            : $" <size=70%>{data.AttackBonus}</size>";

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

        _actionButton.interactable = data.ActionEnabled;
        _actionLabel.text = isMax ? "MAX" : (isUnlock ? "해금" : "레벨업");
        _actionCostText.gameObject.SetActive(!isMax);
        _actionCostIcon.gameObject.SetActive(!isMax);
        _actionCostText.text = data.CostText;

        var costSprite = isUnlock ? _gemCostSprite : _goldCostSprite;
        if (costSprite != null)
            _actionCostIcon.sprite = costSprite;
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
