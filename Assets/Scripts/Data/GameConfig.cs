using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "SlimeTD/Game Config", order = 0)]
public class GameConfig : ScriptableObject
{
    [Header("플레이어")]
    [SerializeField] private int _startingLife = 20;
    [SerializeField] private int _startingJelly = 0;

    [Header("타워 소환")]
    [SerializeField] private int _summonBaseCost = 50;
    [SerializeField] private int _summonCostIncrease = 10;
    [SerializeField] private int _summonGradeId = 1;

    [Header("디버그")]
    [SerializeField] private bool _ignoreJellyCost;
    [SerializeField] private bool _autoStartWave = true;
    [SerializeField] private TowerData _testTower;
    [SerializeField] private int _debugTowerLevel = 1;

    public int StartingLife => _startingLife;
    public int StartingJelly => _startingJelly;
    public int SummonBaseCost => _summonBaseCost;
    public int SummonCostIncrease => _summonCostIncrease;
    public int SummonGradeId => _summonGradeId;
    public bool IgnoreJellyCost => _ignoreJellyCost;
    public bool AutoStartWave => _autoStartWave;
    public TowerData TestTower => _testTower;
    public int DebugTowerLevel => _debugTowerLevel;
}
