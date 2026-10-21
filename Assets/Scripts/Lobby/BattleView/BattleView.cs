using UnityEngine;

public class BattleView : MonoBehaviour
{
    [NotNull][SerializeField] private RectTransform _waveScrollSlot;

    public RectTransform WaveScrollSlot => _waveScrollSlot;
}
