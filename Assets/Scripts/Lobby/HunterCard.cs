using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HunterCard : MonoBehaviour
{
    [NotNull][SerializeField] private Image _border;
    [NotNull][SerializeField] private Image _fill;
    [NotNull][SerializeField] private TMP_Text _nameText;
    [NotNull][SerializeField] private TMP_Text _levelText;
    [NotNull][SerializeField] private TMP_Text _progressText;
    [NotNull][SerializeField] private RectTransform _progressFill;
    [NotNull][SerializeField] private GameObject _progressRoot;
    [NotNull][SerializeField] private GameObject _lockOverlay;
    [NotNull][SerializeField] private GameObject _upgradeBadge;
    [SerializeField] private Button _button;

    // 인덱스는 gradeId - 1이다. 에셋을 입히면 색 대신 프레임 이미지로 바꾼다.
    [SerializeField]
    private Color[] _gradeFillColors =
    {
        new Color(0.75f, 0.72f, 0.65f),
        new Color(0.43f, 0.56f, 0.84f),
        new Color(0.60f, 0.45f, 0.81f),
        new Color(0.88f, 0.71f, 0.29f),
    };

    [SerializeField]
    private Color[] _gradeBorderColors =
    {
        new Color(0.65f, 0.62f, 0.56f),
        new Color(0.29f, 0.43f, 0.72f),
        new Color(0.46f, 0.31f, 0.68f),
        new Color(0.72f, 0.54f, 0.12f),
    };

    [SerializeField] private Color _lockedFillColor = new Color(0.87f, 0.89f, 0.86f);
    [SerializeField] private Color _lockedBorderColor = new Color(0.73f, 0.76f, 0.72f);
    [SerializeField] private Color _nameColor = new Color(0.16f, 0.18f, 0.17f);
    [SerializeField] private Color _lockedNameColor = new Color(0.56f, 0.59f, 0.56f);

    public Button Button => _button;

    public void Set(
        string towerName,
        int gradeId,
        bool unlocked,
        int level,
        bool maxLevel,
        int ownedCards,
        int neededCards,
        bool canLevelUp)
    {
        _nameText.text = towerName;
        _nameText.color = unlocked ? _nameColor : _lockedNameColor;

        int index = Mathf.Clamp(gradeId - 1, 0, _gradeFillColors.Length - 1);
        _fill.color = unlocked ? _gradeFillColors[index] : _lockedFillColor;
        _border.color = unlocked ? _gradeBorderColors[index] : _lockedBorderColor;

        _lockOverlay.SetActive(!unlocked);
        _levelText.gameObject.SetActive(unlocked);
        _progressRoot.SetActive(unlocked);
        _upgradeBadge.SetActive(unlocked && canLevelUp);

        if (!unlocked)
            return;

        _levelText.text = maxLevel ? $"Lv.{level} MAX" : $"Lv.{level}";

        if (maxLevel)
        {
            _progressFill.anchorMax = new Vector2(1f, 1f);
            _progressText.text = "MAX";
            return;
        }

        float ratio = neededCards > 0 ? Mathf.Clamp01((float)ownedCards / neededCards) : 1f;
        _progressFill.anchorMax = new Vector2(ratio, 1f);
        _progressText.text = $"{ownedCards} / {neededCards}";
    }
}