using UnityEngine;
using UnityEngine.Serialization;

// TODO: 서버 연결 전까지 쓰는 임시 데이터. 타워 프리팹은 CodeName으로 찾고, 투사체 모양(프리팹)은 프리팹의 ProjectileLauncher가 가진다
[CreateAssetMenu(fileName = "TowerData_00", menuName = "SlimeTD/Tower Data", order = 61)]
public class TowerData : ScriptableObject
{
    private const float BaseAttackSpeedPercent = 100f;

    [SerializeField] private int _id;
    [SerializeField] private int _gradeId;
    [SerializeField] private bool _enabled = true;
    [SerializeField] private string _codeName;
    [SerializeField] private string _towerName;
    [SerializeField] private TowerAttackType _attackType;
    [SerializeField] private float _attackRange = 5f;

    [Header("기본 수치 (레벨 1)")]
    [SerializeField] private float _baseAttack = 1f;
    [SerializeField] private float _attackGrowthRate = 5f;
    [Tooltip("공격 시작부터 다음 공격 시작까지 간격(초, 공격속도 1 기준)")]
    [FormerlySerializedAs("_attackCooldown")]
    [SerializeField] private float _attackInterval = 1f;
    [SerializeField] private float _critChance = 10f;
    [SerializeField] private float _critBonus = 100f;

    [Header("투사체 (attackType = Projectile)")]
    [SerializeField] private float _projectileSpeed;
    [SerializeField] private float _projectileHitRadius;
    [SerializeField] private float _projectileLifetime;

    [Header("공격 시점 (초, 공격속도 1 기준. 0이면 GameConfig 기본값)")]
    [SerializeField] private float _attackHitDelay;
    [SerializeField] private float _attackDuration;

    [Header("메타 (서버가 판정, 화면 표시용 사본)")]
    [SerializeField] private int _unlockDiamondPrice;
    [SerializeField] private LevelUpCostRule _levelUpCost;

    public int Id => _id;
    public int GradeId => _gradeId;
    public bool Enabled => _enabled;
    public string CodeName => _codeName;
    public string TowerName => _towerName;
    public TowerAttackType AttackType => _attackType;
    public float AttackRange => _attackRange;
    public string ThumbnailName => $"Thumb_{_codeName}";
    public float BaseAttack => _baseAttack;
    public float AttackInterval => _attackInterval;
    public float CritChance => _critChance;
    public float CritBonus => _critBonus;
    public float AttackSpeedPercent => BaseAttackSpeedPercent;
    public ProjectileStats Projectile => new ProjectileStats(_projectileSpeed, _projectileHitRadius, _projectileLifetime);
    public float AttackHitDelay => _attackHitDelay;
    public float AttackDuration => _attackDuration;
    public int UnlockDiamondPrice => _unlockDiamondPrice;
    public LevelUpCostRule LevelUpCost => _levelUpCost;

    public float GetAttack(int level)
    {
        int steps = Mathf.Max(0, level - 1);
        return _baseAttack * (1f + _attackGrowthRate / 100f * steps);
    }
}
