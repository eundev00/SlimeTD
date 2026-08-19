using UnityEngine;

[CreateAssetMenu(fileName = "Wave_00", menuName = "SlimeTD/Wave Data")]
public class WaveData : ScriptableObject
{
    [SerializeField] private int _waveIndex = 1;

    [SerializeField] private float _startDelay = 1f;

    [SerializeField] private SpawnEntry[] _spawnEntries;

    public int WaveIndex => _waveIndex;
    public float StartDelay => _startDelay;
    public SpawnEntry[] SpawnEntries => _spawnEntries;
}
