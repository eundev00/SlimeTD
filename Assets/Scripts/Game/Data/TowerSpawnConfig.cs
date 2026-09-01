using UnityEngine;

[CreateAssetMenu(fileName = "TowerSpawnConfig", menuName = "SlimeTD/Tower Spawn Config", order = 60)]
public class TowerSpawnConfig : ScriptableObject
{
    [SerializeField] private TowerData[] _towerPool;
    [SerializeField] private int _cost = 50;
    [SerializeField] private bool _ignoreGoldCost = true;

    public TowerData[] TowerPool => _towerPool;
    public int Cost => _cost;
    public bool IgnoreGoldCost => _ignoreGoldCost;
}
