using UnityEngine;

// TODO: 서버 연결 전까지 쓰는 임시 데이터. 수치는 SlimeTD-Data의 GameConstants.csv, NewAccount.csv가 원본이다
[CreateAssetMenu(fileName = "GameConfig", menuName = "SlimeTD/Game Config", order = 0)]
public class GameConfig : ScriptableObject
{
    [Header("새 계정 시작 재화 (로비)")]
    [SerializeField] private int _initialCoin = 10000;
    [SerializeField] private int _initialDiamond = 10000;
    [Tooltip("헌터마다 지급하는 시작 카드 수 (임시)")]
    [SerializeField] private int _initialHunterCards = 100;

    [Header("타워 소환")]
    [SerializeField] private int _summonBaseCost = 50;
    [SerializeField] private int _summonCostIncrease = 10;
    [SerializeField] private int _summonGradeId = 1;

    [Header("디버프")]
    [SerializeField, Range(0f, 1f)] private float _slowMinSpeedRatio = 0.25f;
    [SerializeField, Range(0f, 1f)] private float _bossSlowScale = 0.5f;

    [Header("디버그")]
    [SerializeField] private bool _ignoreGoldCost;
    [SerializeField] private bool _autoStartWave = true;
    [SerializeField] private TowerData _testTower;
    [SerializeField] private int _debugTowerLevel = 1;

    public int InitialCoin => _initialCoin;
    public int InitialDiamond => _initialDiamond;
    public int InitialHunterCards => _initialHunterCards;
    public int SummonBaseCost => _summonBaseCost;
    public int SummonCostIncrease => _summonCostIncrease;
    public int SummonGradeId => _summonGradeId;
    public float SlowMinSpeedRatio => _slowMinSpeedRatio;
    public float BossSlowScale => _bossSlowScale;
    public bool IgnoreGoldCost => _ignoreGoldCost;
    public bool AutoStartWave => _autoStartWave;
    public TowerData TestTower => _testTower;
    public int DebugTowerLevel => _debugTowerLevel;
}
