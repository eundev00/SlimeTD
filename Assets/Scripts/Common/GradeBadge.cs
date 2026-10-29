using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GradeBadge : MonoBehaviour
{
    [NotNull][SerializeField] private GradeStyle _style;
    [NotNull][SerializeField] private Image _fill;
    [NotNull][SerializeField] private Image _border;
    [NotNull][SerializeField] private TMP_Text _label;

    public string Label => _label.text;

    public void Show(int gradeId)
    {
        if (!_style.TryGetColors(gradeId, out var colors))
        {
            Debug.Log($"[GradeBadge] 등급 스타일이 없다: {gradeId}", this);
            _label.text = string.Empty;
            return;
        }

        _label.text = colors.Name;
        _fill.color = colors.Fill;
        _border.color = colors.Border;
    }
}
