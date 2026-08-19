using UnityEngine;

[CreateAssetMenu(fileName = "WaveTable", menuName = "SlimeTD/Wave Table")]
public class WaveTableData : ScriptableObject
{
    [SerializeField] private WaveData[] _waves;

    public WaveData[] Waves => _waves;
}
