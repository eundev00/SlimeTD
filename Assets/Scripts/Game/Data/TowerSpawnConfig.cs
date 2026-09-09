using UnityEngine;

[CreateAssetMenu(fileName = "TowerSpawnConfig", menuName = "SlimeTD/Tower Spawn Config", order = 60)]
public class TowerSpawnConfig : ScriptableObject
{
    [SerializeField] private TowerData[] _towerPool;
    [SerializeField] private int _cost = 50;

    public TowerData[] TowerPool => _towerPool;
    public int Cost => _cost;
}
