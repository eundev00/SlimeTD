using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardItemView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _amountText;

    public void Setup(Sprite icon, long amount)
    {
        if (_icon != null)
            _icon.sprite = icon;

        if (_amountText != null)
            _amountText.text = amount.ToString("N0");
    }
}
