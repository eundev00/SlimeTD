using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HunterCardRevealCardView : MonoBehaviour
{
    private const int StrongGradeId = 3;

    [NotNull][SerializeField] private GradeStyle _style;
    [NotNull][SerializeField] private Button _button;
    [NotNull][SerializeField] private RectTransform _flipper;
    [NotNull][SerializeField] private GameObject _back;
    [NotNull][SerializeField] private Image _backGlow;
    [NotNull][SerializeField] private GameObject _front;
    [NotNull][SerializeField] private Image _frontBg;
    [NotNull][SerializeField] private Image _frontBorder;
    [NotNull][SerializeField] private Image _thumbnail;
    [NotNull][SerializeField] private TMP_Text _nameText;
    [NotNull][SerializeField] private Image[] _sparks;

    [SerializeField] private float _sparkDistance = 0.6f;

    private Sequence _sequence;
    private Tween _glowTween;
    private Color _accent = Color.white;
    private int _gradeId;

    public event Action<HunterCardRevealCardView> Clicked;

    private void Start()
    {
        if (_button == null)
        {
            Debug.Log("[HunterCardRevealCardView] 버튼이 연결되지 않았습니다.", this);
            return;
        }

        _button.onClick.AddListener(OnClicked);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnClicked);

        KillTweens();
    }

    public void Setup(HunterCardRevealItem item)
    {
        KillTweens();

        _gradeId = item.GradeId;
        if (_style.TryGetColors(item.GradeId, out var colors))
        {
            _frontBg.color = colors.Fill;
            _frontBorder.color = colors.Border;
            _accent = colors.Border;
        }

        _thumbnail.sprite = item.Thumbnail;
        _thumbnail.enabled = item.Thumbnail != null;
        _nameText.text = item.Name;

        _back.SetActive(true);
        _front.SetActive(false);
        _flipper.localScale = Vector3.zero;

        foreach (var spark in _sparks)
            spark.gameObject.SetActive(false);

        // 1등급은 뒷면 빛이 없다. 빛 색으로 뒤집기 전에 등급을 미리 알려준다.
        bool glow = item.GradeId > 1;
        _backGlow.gameObject.SetActive(glow);
        if (glow)
        {
            var glowColor = _accent;
            glowColor.a = 0.55f;
            _backGlow.color = glowColor;
            _glowTween = _backGlow.DOFade(1f, 0.7f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject);
        }
    }

    public void PlayAppear(float delay)
    {
        _sequence?.Kill();
        _flipper.localScale = Vector3.zero;
        _sequence = DOTween.Sequence()
            .AppendInterval(delay)
            .Append(_flipper.DOScale(1.12f, 0.195f).SetEase(Ease.OutQuad))
            .Append(_flipper.DOScale(0.96f, 0.06f).SetEase(Ease.InOutQuad))
            .Append(_flipper.DOScale(1f, 0.045f).SetEase(Ease.OutQuad))
            .SetLink(gameObject);
    }

    public void Flip()
    {
        bool strong = _gradeId >= StrongGradeId;
        float settle = strong ? 0.38f : 0.26f;

        _sequence?.Kill();
        _flipper.localScale = Vector3.one;
        _sequence = DOTween.Sequence()
            .Append(_flipper.DOScale(new Vector3(0f, 1.04f, 1f), 0.11f).SetEase(Ease.InQuad))
            .AppendCallback(ShowFront)
            .Append(_flipper.DOScale(strong ? new Vector3(1.18f, 0.94f, 1f) : new Vector3(1.1f, 0.96f, 1f), settle * 0.55f).SetEase(Ease.OutQuad))
            .Append(_flipper.DOScale(new Vector3(0.97f, 1.03f, 1f), settle * 0.25f).SetEase(Ease.InOutQuad))
            .Append(_flipper.DOScale(Vector3.one, settle * 0.2f).SetEase(Ease.OutQuad))
            .SetLink(gameObject);
    }

    public void ResetView()
    {
        KillTweens();
        _flipper.localScale = Vector3.zero;
    }

    private void ShowFront()
    {
        _glowTween?.Kill();
        _back.SetActive(false);
        _front.SetActive(true);
        PlaySparks();
    }

    private void PlaySparks()
    {
        if (_gradeId <= 1)
            return;

        int count = _gradeId >= StrongGradeId ? _sparks.Length : Mathf.Min(_sparks.Length, _sparks.Length / 2 + 1);
        float distance = ((RectTransform)transform).rect.width * _sparkDistance * (_gradeId >= StrongGradeId ? 1.35f : 1f);

        for (int i = 0; i < count; i++)
        {
            var spark = _sparks[i];
            var rect = spark.rectTransform;
            float angle = Mathf.PI * 2f * i / count + UnityEngine.Random.Range(0f, 0.4f);
            float dist = distance * UnityEngine.Random.Range(0.7f, 1.3f);
            float duration = UnityEngine.Random.Range(0.5f, 0.7f);

            spark.DOKill();
            rect.DOKill();
            spark.gameObject.SetActive(true);
            spark.color = _accent;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;

            rect.DOAnchorPos(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist, duration).SetEase(Ease.OutCubic).SetLink(spark.gameObject);
            rect.DOScale(0.2f, duration).SetEase(Ease.OutCubic).SetLink(spark.gameObject);
            spark.DOFade(0f, duration).SetEase(Ease.OutCubic).SetLink(spark.gameObject)
                .OnComplete(() => spark.gameObject.SetActive(false));
        }
    }

    private void KillTweens()
    {
        _sequence?.Kill();
        _sequence = null;
        _glowTween?.Kill();
        _glowTween = null;

        foreach (var spark in _sparks)
        {
            if (spark == null)
                continue;

            spark.DOKill();
            spark.rectTransform.DOKill();
            spark.gameObject.SetActive(false);
        }
    }

    private void OnClicked()
    {
        Clicked?.Invoke(this);
    }
}
