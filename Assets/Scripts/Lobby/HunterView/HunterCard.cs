using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HunterCard : MonoBehaviour
{
    [NotNull][SerializeField] private GradeStyle _gradeStyle;
    [NotNull][SerializeField] private Image _frameBackground;
    [NotNull][SerializeField] private Image _frameBorder;
    [NotNull][SerializeField] private Image _frameOutline;
    [NotNull][SerializeField] private Image _thumbnail;
    [SerializeField] private Color _lockedThumbnailColor = new Color(0.6f, 0.6f, 0.6f, 0.35f);

    [NotNull][SerializeField] private TMP_Text _nameText;
    [NotNull][SerializeField] private TMP_Text _levelText;
    [NotNull][SerializeField] private GameObject _upgradeBadge;

    [NotNull][SerializeField] private Image _gaugeBackground;
    [NotNull][SerializeField] private Slider _progressSlider;
    [NotNull][SerializeField] private Image _progressFill;
    [NotNull][SerializeField] private TMP_Text _progressText;
    [NotNull][SerializeField] private GameObject _progressLock;
    [SerializeField] private Color _gaugeEmptyColor = new Color32(0x34, 0x40, 0x4C, 0xFF);
    [SerializeField] private Color _gaugeLockedColor = new Color32(0x86, 0x82, 0x83, 0xFF);
    [SerializeField] private Color _gaugePartialColor = new Color32(0xFF, 0xD4, 0x3D, 0xFF);
    [SerializeField] private Color _gaugeFullColor = new Color32(0x15, 0xEE, 0x8A, 0xFF);

    [NotNull][SerializeField] private Button _button;

    public Button Button => _button;

    public void Set(
        string towerName,
        Sprite thumbnail,
        int gradeId,
        bool unlocked,
        int level,
        bool maxLevel,
        int ownedCards,
        int neededCards,
        bool canLevelUp)
    {
        _nameText.text = towerName;

        _thumbnail.sprite = thumbnail;
        _thumbnail.enabled = thumbnail != null;
        _thumbnail.color = unlocked ? Color.white : _lockedThumbnailColor;

        ApplyFrameColors(unlocked ? GetGradeColors(gradeId) : _gradeStyle.Locked);

        _levelText.gameObject.SetActive(unlocked);
        _upgradeBadge.SetActive(unlocked && canLevelUp);
        _progressLock.SetActive(!unlocked);
        _progressText.gameObject.SetActive(unlocked);

        if (!unlocked)
        {
            _gaugeBackground.color = _gaugeLockedColor;
            _progressSlider.value = 0f;
            return;
        }

        _levelText.text = $"Lv. {level}";
        _gaugeBackground.color = _gaugeEmptyColor;

        float ratio = maxLevel || neededCards <= 0 ? 1f : Mathf.Clamp01((float)ownedCards / neededCards);
        _progressSlider.value = ratio;
        _progressFill.color = ratio >= 1f ? _gaugeFullColor : _gaugePartialColor;
        _progressText.text = maxLevel ? "MAX" : $"{ownedCards}/{neededCards}";
    }

    private GradeStyle.GradeColors GetGradeColors(int gradeId)
    {
        if (_gradeStyle.TryGetColors(gradeId, out var colors))
            return colors;

        Debug.Log($"[HunterCard] 등급 스타일이 없다: {gradeId}", this);
        return null;
    }

    private void ApplyFrameColors(GradeStyle.GradeColors colors)
    {
        if (colors == null)
            return;

        _frameBackground.color = colors.Fill;
        _frameBorder.color = colors.Border;
        _frameOutline.color = colors.Outline;
    }
}
