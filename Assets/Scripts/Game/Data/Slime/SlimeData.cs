using UnityEngine;

[CreateAssetMenu(fileName = "SlimeData_00", menuName = "SlimeTD/Slime Data", order = 40)]
public class SlimeData : SlimeDataBase
{
    [SerializeField] private int _appearFromWave = 1;
    [SerializeField] private int _spawnWeight = 10;

    public int AppearFromWave => _appearFromWave;
    public int SpawnWeight => _spawnWeight;
}
