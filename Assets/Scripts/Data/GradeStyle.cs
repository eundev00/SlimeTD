using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GradeStyle", menuName = "SlimeTD/Grade Style", order = 63)]
public class GradeStyle : ScriptableObject
{
    [Serializable]
    public class GradeColors
    {
        [SerializeField] private Color _fill = Color.white;
        [SerializeField] private Color _border = Color.white;

        public Color Fill => _fill;
        public Color Border => _border;
    }

    // 인덱스 0 = 등급 1
    [SerializeField] private GradeColors[] _colors;

    public bool TryGetColors(int gradeId, out GradeColors colors)
    {
        int index = gradeId - 1;

        if (_colors == null || index < 0 || index >= _colors.Length)
        {
            colors = null;
            return false;
        }

        colors = _colors[index];
        return colors != null;
    }
}
