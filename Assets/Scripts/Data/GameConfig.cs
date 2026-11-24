using UnityEngine;

// 수치는 SlimeTD-Data의 GameConstants.csv가 원본이다. 서버도 같은 값으로 싱글 전투를 다시 계산한다
[CreateAssetMenu(fileName = "GameConfig", menuName = "SlimeTD/Game Config", order = 0)]
public class GameConfig : ScriptableObject
{
    [Header("타워 소환")]
    [SerializeField] private int _summonBaseCost = 50;
    [SerializeField] private int _summonCostIncrease = 10;
    [SerializeField] private int _summonGradeId = 1;

    [Header("디버프")]
    [SerializeField, Range(0f, 1f)] private float _slowMinSpeedRatio = 0.25f;
    [SerializeField, Range(0f, 1f)] private float _bossSlowScale = 0.5f;

    [Header("에너지 (표시용, 서버가 확정)")]
    [SerializeField] private int _energyCostPerBattle = 5;

    [Header("디버그")]
    [SerializeField] private bool _ignoreGoldCost;
    [SerializeField] private bool _autoStartWave = true;
    [SerializeField] private TowerData _testTower;
    [SerializeField] private int _debugTowerLevel = 1;

    public int SummonBaseCost => _summonBaseCost;
    public int SummonCostIncrease => _summonCostIncrease;
    public int SummonGradeId => _summonGradeId;
    public float SlowMinSpeedRatio => _slowMinSpeedRatio;
    public float BossSlowScale => _bossSlowScale;
    public int EnergyCostPerBattle => _energyCostPerBattle;
    public bool IgnoreGoldCost => _ignoreGoldCost;
    public bool AutoStartWave => _autoStartWave;
    public TowerData TestTower => _testTower;
    public int DebugTowerLevel => _debugTowerLevel;
}
