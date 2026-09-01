using UnityEngine;

[CreateAssetMenu(fileName = "SpawnEntry_00", menuName = "SlimeTD/Spawn Entry", order = 20)]
public class SpawnEntry : ScriptableObject
{
    [SerializeField] private SlimeData[] _slimeDatas;
    [SerializeField] private float _spawnInterval = 1f;

    public SlimeData[] SlimeDatas => _slimeDatas;
    public float SpawnInterval => _spawnInterval;
}
