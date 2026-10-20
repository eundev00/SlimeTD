using System;
using TMPro;
using UniRx;
using UnityEngine;

public class HunterTab : MonoBehaviour
{
    [NotNull][SerializeField] private RectTransform _grid;
    [NotNull][SerializeField] private HunterCard _cardPrefab;
    [NotNull][SerializeField] private TMP_Text _cardsText;
    [NotNull][SerializeField] private SortButton _sortButton;

    public IObservable<Unit> SortClicked => _sortButton.Clicked;

    public HunterCard CreateCard()
    {
        return Instantiate(_cardPrefab, _grid);
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
