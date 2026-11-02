using System;
using UniRx;
using UnityEngine;

public class HunterCardView : MonoBehaviour
{
    [NotNull][SerializeField] private RectTransform _grid;
    [NotNull][SerializeField] private SortButton _sortButton;

    public IObservable<Unit> SortClicked => _sortButton.Clicked;

    public HunterCard CreateCard(HunterCard prefab)
    {
        return Instantiate(prefab, _grid);
    }

    public void SetSort(string label, bool ascending)
    {
        _sortButton.Set(label, ascending);
    }
}
