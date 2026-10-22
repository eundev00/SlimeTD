using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GradeBadge : MonoBehaviour
{
    [NotNull][SerializeField] private GradeStyle _style;
    [NotNull][SerializeField] private Image _fill;
    [NotNull][SerializeField] private Image _border;
    [NotNull][SerializeField] private TMP_Text _label;

    public void Show(int gradeId, string gradeName)
    {
        _label.text = gradeName;

        if (!_style.TryGetColors(gradeId, out var colors))
        {
            Debug.Log($"[GradeBadge] 등급 스타일이 없다: {gradeId}", this);
            return;
        }

        _fill.color = colors.Fill;
        _border.color = colors.Border;
    }
}
