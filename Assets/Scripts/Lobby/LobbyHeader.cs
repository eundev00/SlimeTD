using TMPro;
using UnityEngine;

public class LobbyHeader : MonoBehaviour
{
    [NotNull][SerializeField] private TMP_Text _goldText;
    [NotNull][SerializeField] private TMP_Text _gemText;

    public void SetGold(int gold)
    {
        if (_goldText != null)
            _goldText.text = gold.ToString("N0");
    }

    public void SetGem(int gem)
    {
        if (_gemText != null)
            _gemText.text = gem.ToString("N0");
    }
}