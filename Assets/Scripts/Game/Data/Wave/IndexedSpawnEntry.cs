using UnityEngine;

[CreateAssetMenu(fileName = "IndexedSpawnEntry_00", menuName = "SlimeTD/Indexed Spawn Entry")]
public class IndexedSpawnEntry : SpawnEntry
{
    [SerializeField] private int _waveIndex = 1;

    public int WaveIndex => _waveIndex;
}
