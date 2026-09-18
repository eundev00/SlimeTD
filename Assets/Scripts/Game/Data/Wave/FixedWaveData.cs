using UnityEngine;

[CreateAssetMenu(fileName = "Wave_00", menuName = "SlimeTD/Fixed Wave", order = 22)]
public class FixedWaveData : ScriptableObject
{
    [SerializeField] private int _waveNumber = 1;
    [SerializeField] private SlimeData[] _slimes;

    public int WaveNumber => _waveNumber;
    public SlimeData[] Slimes => _slimes;
}
