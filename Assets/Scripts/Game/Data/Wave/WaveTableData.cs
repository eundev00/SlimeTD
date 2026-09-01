using UnityEngine;

[CreateAssetMenu(fileName = "WaveTable", menuName = "SlimeTD/Wave Table", order = 23)]
public class WaveTableData : ScriptableObject
{
    [SerializeField] private int _maxWave = 10;
    [SerializeField] private IndexedSpawnEntry[] _manualWave;
    [SerializeField] private IndexedSpawnEntry[] _bossWave;
    [SerializeField] private SpawnEntry[] _autoWave;
    [SerializeField] private float _countRate = 0.1f;
    [SerializeField] private float _healthRate = 0.15f;

    [SerializeField] private int _maxSlimeCount = 30;

    public int MaxWave => _maxWave;
    public IndexedSpawnEntry[] ManualWave => _manualWave;
    public IndexedSpawnEntry[] BossWave => _bossWave;
    public SpawnEntry[] AutoWave => _autoWave;
    public float CountRate => _countRate;
    public float HealthRate => _healthRate;
    public int MaxSlimeCount => _maxSlimeCount;
}
