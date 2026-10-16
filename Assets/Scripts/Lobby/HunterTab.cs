using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class HunterTab : MonoBehaviour
{
    [NotNull][SerializeField] private RectTransform _grid;
    [NotNull][SerializeField] private HunterCard _cardPrefab;
    [NotNull][SerializeField] private TMP_Text _cardsText;
    [NotNull][SerializeField] private Button _sortButton;
    [NotNull][SerializeField] private TMP_Text _sortLabel;

    public IObservable<Unit> SortClicked => _sortButton.OnClickAsObservable();

    public HunterCard CreateCard()
    {
        return Instantiate(_cardPrefab, _grid);
    }

    public void SetCards(int cards)
    {
        _cardsText.text = cards.ToString("N0");
    }

    public void SetSortLabel(string label)
    {
        _sortLabel.text = label;
    }
}