using DG.Tweening;
using Services.PoolService;
using TMPro;
using UnityEngine;
using VContainer;

public class DamageTextView : MonoBehaviour, IPoolItem
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private RectTransform _rectTransform;

    private const float Duration = 1.5f;
    private const float FadeDuration = 0.5f;
    private const float RiseDistance = 40f;
    private static readonly Vector3 WorldOffset = new Vector3(0f, 1f, 0f);

    private Sequence _sequence;
    private IGameObjectPoolService _poolService;

    [Inject]
    public void Construct(IGameObjectPoolService poolService)
    {
        _poolService = poolService;
    }

    public void Initialize(int damage, Vector3 worldPosition, Canvas canvas, Camera camera)
    {
        KillSequence();

        _text.text = damage.ToString();

        Vector3 screenPos = camera.WorldToScreenPoint(worldPosition + WorldOffset);
        screenPos.x -= Screen.width * 0.5f;
        screenPos.y -= Screen.height * 0.5f;
        Vector2 anchoredPos = (Vector2)screenPos / canvas.scaleFactor;

        _rectTransform.anchoredPosition = anchoredPos;

        _sequence = DOTween.Sequence()
            .Append(_rectTransform.DOAnchorPosY(anchoredPos.y + RiseDistance, Duration).SetEase(Ease.OutCubic))
            .Join(_text.DOFade(0f, FadeDuration).SetDelay(Duration - FadeDuration))
            .SetLink(gameObject)
            .OnComplete(() => _poolService.Release(gameObject));
    }

    public void OnGetFromPool()
    {
        KillSequence();
        transform.localScale = Vector3.one;
    }

    public void OnReturnToPool()
    {
        KillSequence();
        _text.alpha = 1f;
    }

    private void OnDestroy()
    {
        KillSequence();
    }

    private void KillSequence()
    {
        if (_sequence == null)
            return;

        _sequence.Kill(false);
        _sequence = null;
    }
}
