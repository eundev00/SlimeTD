using UnityEngine;

[CreateAssetMenu(fileName = "TowerSpawnConfig", menuName = "SlimeTD/Tower Spawn Config")]
public class TowerSpawnConfig : ScriptableObject
{
    [SerializeField] private GameObject _towerPrefab;
    [SerializeField] private int _cost = 50;
    [SerializeField] private bool _ignoreGoldCost = true;

    public GameObject TowerPrefab => _towerPrefab;
    public int Cost => _cost;
    public bool IgnoreGoldCost => _ignoreGoldCost;
}
