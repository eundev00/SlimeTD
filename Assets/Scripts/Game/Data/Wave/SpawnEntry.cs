using UnityEngine;

[CreateAssetMenu(fileName = "SpawnEntry_00", menuName = "SlimeTD/Spawn Entry")]
public class SpawnEntry : ScriptableObject
{
    [SerializeField] private GameObject[] _slimePrefabs;
    [SerializeField] private float _spawnInterval = 1f;

    public GameObject[] SlimePrefabs => _slimePrefabs;
    public float SpawnInterval => _spawnInterval;
}
