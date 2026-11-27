using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GradeStyle", menuName = "SlimeTD/Grade Style", order = 63)]
public class GradeStyle : ScriptableObject
{
    [Serializable]
    public class GradeColors
    {
        [SerializeField] private string _name;
        [SerializeField] private Color _fill = Color.white;
        [SerializeField] private Color _border = Color.white;
        [SerializeField] private Color _outline = Color.black;

        public string Name => _name;
        // 바탕, 안쪽 테두리, 바깥 외곽선
        public Color Fill => _fill;
        public Color Border => _border;
        public Color Outline => _outline;
    }

    // 인덱스 0 = 등급 1
    [SerializeField] private GradeColors[] _colors;
    [SerializeField] private GradeColors _locked;

    public GradeColors Locked => _locked;

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
