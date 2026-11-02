using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HunterCard : MonoBehaviour
{
    [NotNull][SerializeField] private GradeStyle _gradeStyle;
    [NotNull][SerializeField] private GameObject _gradeFrame;
    [NotNull][SerializeField] private Image _frameBackground;
    [NotNull][SerializeField] private Image _frameBorder;
    [NotNull][SerializeField] private GameObject _lockedFrame;
    [NotNull][SerializeField] private Image _thumbnail;
    [NotNull][SerializeField] private TMP_Text _nameText;
    [NotNull][SerializeField] private TMP_Text _levelText;
    [NotNull][SerializeField] private TMP_Text _progressText;
    [NotNull][SerializeField] private Slider _progressSlider;
    [NotNull][SerializeField] private GameObject _upgradeBadge;
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

        _gradeFrame.SetActive(unlocked);
        if (unlocked)
            ApplyGradeColors(gradeId);

        _lockedFrame.SetActive(!unlocked);
        _levelText.gameObject.SetActive(unlocked);
        _upgradeBadge.SetActive(unlocked && canLevelUp);

        if (!unlocked)
        {
            _progressSlider.value = 0f;
            _progressText.text = string.Empty;
            return;
        }

        _levelText.text = $"Lv.{level}";

        if (maxLevel)
        {
            _progressSlider.value = 1f;
            _progressText.text = "MAX";
        }
        else
        {
            float ratio = neededCards > 0 ? Mathf.Clamp01((float)ownedCards / neededCards) : 1f;
            _progressSlider.value = ratio;
            _progressText.text = $"{ownedCards}/{neededCards}";
        }

    }

    private void ApplyGradeColors(int gradeId)
    {
        if (!_gradeStyle.TryGetColors(gradeId, out var colors))
        {
            Debug.Log($"[HunterCard] 등급 스타일이 없다: {gradeId}", this);
            return;
        }

        _frameBackground.color = colors.Fill;
        _frameBorder.color = colors.Border;
    }
}
