using System;
using TMPro;
using UniRx;
using UnityEngine;

public class HunterCardView : MonoBehaviour
{
    [NotNull][SerializeField] private RectTransform _grid;
    [NotNull][SerializeField] private TMP_Text _cardsText;
    [NotNull][SerializeField] private SortButton _sortButton;

    public IObservable<Unit> SortClicked => _sortButton.Clicked;

    public HunterCard CreateCard(HunterCard prefab)
    {
        return Instantiate(prefab, _grid);
    }

    public void SetCards(int cards)
    {
        _cardsText.text = cards.ToString("N0");
    }

    public void SetSort(string label, bool ascending)
    {
        _sortButton.Set(label, ascending);
    }
}
