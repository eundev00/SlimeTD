using UnityEngine;

[CreateAssetMenu(fileName = "SpawnEntry_00", menuName = "SlimeTD/Spawn Entry")]
public class SpawnEntry : ScriptableObject
{
    [SerializeField] private GameObject _slimePrefab;
    [SerializeField] private int _count = 1;
    [SerializeField] private float _spawnInterval = 1f;

    public GameObject SlimePrefab => _slimePrefab;
    public int Count => _count;
    public float SpawnInterval => _spawnInterval;
}
