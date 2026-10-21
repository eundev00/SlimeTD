using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class SortButton : MonoBehaviour
{
    [NotNull][SerializeField] private Button _button;
    [NotNull][SerializeField] private TMP_Text _label;
    [NotNull][SerializeField] private RectTransform _arrow;

    public IObservable<Unit> Clicked => _button.OnClickAsObservable();

    // 화살표 스프라이트는 아래를 향한다. 오름차순이면 180도 뒤집는다.
    public void Set(string label, bool ascending)
    {
        _label.text = label;
        _arrow.localEulerAngles = new Vector3(0f, 0f, ascending ? 180f : 0f);
    }
}
