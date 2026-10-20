using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleMap : MonoBehaviour
{
    [NotNull][SerializeField] private ScrollRect _scroll;
    [NotNull][SerializeField] private RectTransform _content;
    [NotNull][SerializeField] private RectTransform _playerMarker;
    [NotNull][SerializeField] private TMP_Text _bestWaveText;
    [NotNull][SerializeField] private CanvasGroup _completeGroup;
    [SerializeField] private float _baseOffset = 330f;
    [SerializeField] private float _pixelsPerWave = 112f;
    [SerializeField] private int _maxWave = 50;
    [SerializeField] private float _incompleteAlpha = 0.45f;

    public void SetBestWave(int wave)
    {
        Canvas.ForceUpdateCanvases();

        wave = Mathf.Clamp(wave, 0, _maxWave);

        float y = _baseOffset + wave * _pixelsPerWave;
        _playerMarker.anchoredPosition = new Vector2(_playerMarker.anchoredPosition.x, y);
        _bestWaveText.text = wave == 0 ? "도전 시작!" : $"최고 웨이브 {wave}";
        _completeGroup.alpha = wave >= _maxWave ? 1f : _incompleteAlpha;

        ScrollTo(y);
    }

    // 최고 웨이브가 화면 아래쪽 40% 지점에 오도록 맞춘다. 콘텐츠는 아래 기준 피벗이라 위로 올린 만큼 음수 y다.
    private void ScrollTo(float contentY)
    {
        float viewHeight = ((RectTransform)_scroll.viewport).rect.height;
        float scrollable = _content.rect.height - viewHeight;
        if (scrollable <= 0f)
            return;

        float bottom = Mathf.Clamp(contentY - viewHeight * 0.4f, 0f, scrollable);
        _scroll.StopMovement();
        _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, -bottom);
    }
}
