using TMPro;
using UnityEngine;

public class HudView : MonoBehaviour
{
    [NotNull][SerializeField] private TMP_Text _goldText;
    [NotNull][SerializeField] private TMP_Text _lifeText;
    [NotNull][SerializeField] private TMP_Text _waveText;

    public void SetGold(int value)
    {
        _goldText.text = value.ToString();
    }

    public void SetLife(int value)
    {
        _lifeText.text = value.ToString();
    }

    public void SetWave(int value)
    {
        _waveText.text = $"웨이브 {value}";
    }
}
