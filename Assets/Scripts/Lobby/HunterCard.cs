using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HunterCard : MonoBehaviour
{
    // 인덱스는 gradeId - 1이다.
    [NotNull][SerializeField] private GameObject[] _gradeFrames;
    [NotNull][SerializeField] private GameObject _lockedFrame;
    [NotNull][SerializeField] private TMP_Text _nameText;
    [NotNull][SerializeField] private TMP_Text _levelText;
    [NotNull][SerializeField] private TMP_Text _progressText;
    [NotNull][SerializeField] private RectTransform _progressFill;
    [NotNull][SerializeField] private GameObject _lockedBar;
    [NotNull][SerializeField] private GameObject _upgradeBadge;
    [SerializeField] private Button _button;

    [SerializeField] private Color _nameColor = new Color(0.35f, 0.24f, 0.17f);
    [SerializeField] private Color _lockedNameColor = new Color(0.55f, 0.48f, 0.38f);

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

        int index = Mathf.Clamp(gradeId - 1, 0, _gradeFrames.Length - 1);
        for (int i = 0; i < _gradeFrames.Length; i++)
            _gradeFrames[i].SetActive(unlocked && i == index);

        _lockedFrame.SetActive(!unlocked);
        _lockedBar.SetActive(!unlocked);
        _levelText.gameObject.SetActive(unlocked);
        _upgradeBadge.SetActive(unlocked && canLevelUp);

        if (!unlocked)
        {
            _progressText.text = string.Empty;
            return;
        }

        _levelText.text = maxLevel ? $"Lv.{level} MAX" : $"Lv.{level}";

        if (maxLevel)
        {
            _progressFill.anchorMax = new Vector2(1f, 1f);
            _progressText.text = "MAX";
            return;
        }

        float ratio = neededCards > 0 ? Mathf.Clamp01((float)ownedCards / neededCards) : 1f;
        _progressFill.anchorMax = new Vector2(ratio, 1f);
        _progressText.text = $"{ownedCards}/{neededCards}";
    }
}
