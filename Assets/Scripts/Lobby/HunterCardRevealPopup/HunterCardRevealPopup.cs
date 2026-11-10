using System;
using System.Collections.Generic;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

[PopupPresenter(typeof(HunterCardRevealPresenter))]
public class HunterCardRevealPopup : PopupBase
{
    private const float AppearStartDelay = 0.15f;
    private const float AppearInterval = 0.04f;

    [NotNull][SerializeField] private RectTransform _cardRoot;
    [NotNull][SerializeField] private HunterCardRevealCardView _cardTemplate;
    [NotNull][SerializeField] private Button _actionButton;
    [NotNull][SerializeField] private TMP_Text _actionText;

    [Header("여러 장 배치 (3-4-3 부채꼴)")]
    [SerializeField] private Vector2 _multiCardSize = new Vector2(240f, 300f);
    [SerializeField] private float _columnStep = 262f;
    [SerializeField] private float _rowGap = 40f;
    [SerializeField] private float _rowCurve = 12f;
    [SerializeField] private float _tiltPerColumn = 4f;

    [Header("한 장 배치")]
    [SerializeField] private Vector2 _singleCardSize = new Vector2(420f, 525f);

    private readonly ReactiveProperty<HunterCardGachaResult> _result = new ReactiveProperty<HunterCardGachaResult>();
    private readonly List<HunterCardRevealCardView> _cards = new List<HunterCardRevealCardView>();
    private int _visibleCount;

    public IReadOnlyReactiveProperty<HunterCardGachaResult> Result => _result;

    public event Action<int> CardClicked;
    public event Action ActionClicked;

    public void SetResult(HunterCardGachaResult result)
    {
        _result.SetValueAndForceNotify(result);
    }

    private void Awake()
    {
        _cardTemplate.gameObject.SetActive(false);
    }

    private void Start()
    {
        _actionButton.onClick.AddListener(OnActionClicked);
    }

    private void OnDestroy()
    {
        if (_actionButton != null)
            _actionButton.onClick.RemoveListener(OnActionClicked);

        foreach (var card in _cards)
        {
            if (card != null)
                card.Clicked -= OnCardClicked;
        }

        _result.Dispose();
    }

    protected override void OnClose()
    {
        foreach (var card in _cards)
        {
            card.ResetView();
            card.gameObject.SetActive(false);
        }

        _visibleCount = 0;
        _result.Value = null;
    }

    // 결과를 다 보기 전에 뒤로가기로 닫히지 않도록 버튼과 같은 동작을 한다
    protected override void OnBackPressed()
    {
        OnActionClicked();
    }

    public void ShowCards(IReadOnlyList<HunterCardRevealItem> items)
    {
        while (_cards.Count < items.Count)
        {
            var card = Instantiate(_cardTemplate, _cardRoot);
            card.Clicked += OnCardClicked;
            _cards.Add(card);
        }

        var layouts = BuildLayout(items.Count);
        for (int i = 0; i < _cards.Count; i++)
        {
            var card = _cards[i];
            bool visible = i < items.Count;
            card.gameObject.SetActive(visible);
            if (!visible)
                continue;

            var rect = (RectTransform)card.transform;
            rect.sizeDelta = layouts[i].Size;
            rect.anchoredPosition = layouts[i].Position;
            rect.localRotation = Quaternion.Euler(0f, 0f, layouts[i].Angle);

            card.Setup(items[i]);
            card.PlayAppear(AppearStartDelay + i * AppearInterval);
        }

        _visibleCount = items.Count;
    }

    public void Flip(int index)
    {
        if (index < 0 || index >= _visibleCount)
            return;

        _cards[index].Flip();
    }

    public void SetActionText(string text)
    {
        _actionText.text = text;
    }

    private List<CardLayout> BuildLayout(int count)
    {
        var result = new List<CardLayout>(count);
        if (count == 1)
        {
            result.Add(new CardLayout(Vector2.zero, _singleCardSize, 0f));
            return result;
        }

        var rows = GetRowCounts(count);
        float height = rows.Count * _multiCardSize.y + (rows.Count - 1) * _rowGap;
        float rowCenterY = height * 0.5f - _multiCardSize.y * 0.5f;

        foreach (int n in rows)
        {
            for (int i = 0; i < n; i++)
            {
                float t = i - (n - 1) * 0.5f;
                var position = new Vector2(t * _columnStep, rowCenterY - t * t * _rowCurve);
                result.Add(new CardLayout(position, _multiCardSize, -t * _tiltPerColumn));
            }

            rowCenterY -= _multiCardSize.y + _rowGap;
        }

        return result;
    }

    private static List<int> GetRowCounts(int count)
    {
        if (count == 10)
            return new List<int> { 3, 4, 3 };

        var rows = new List<int>();
        for (int left = count; left > 0; left -= 4)
            rows.Add(Mathf.Min(4, left));

        return rows;
    }

    private void OnCardClicked(HunterCardRevealCardView card)
    {
        int index = _cards.IndexOf(card);
        if (index >= 0)
            CardClicked?.Invoke(index);
    }

    private void OnActionClicked()
    {
        ActionClicked?.Invoke();
    }

    private readonly struct CardLayout
    {
        public readonly Vector2 Position;
        public readonly Vector2 Size;
        public readonly float Angle;

        public CardLayout(Vector2 position, Vector2 size, float angle)
        {
            Position = position;
            Size = size;
            Angle = angle;
        }
    }
}
