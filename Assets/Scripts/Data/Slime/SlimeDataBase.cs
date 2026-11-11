using UnityEngine;

// TODO: 서버 연결 전까지 쓰는 임시 데이터. 슬라임 프리팹은 CodeName과 같은 이름의 프리팹을 찾는다
public abstract class SlimeDataBase : ScriptableObject
{
    [SerializeField] private int _slimeId;
    [SerializeField] private string _codeName;
    [Tooltip("일반: 웨이브 성장률 적용 전 기본값 / 보스: 이 값 그대로 사용")]
    [SerializeField] private int _baseHealth = 3;
    [SerializeField] private float _baseSpeed = 1f;
    [SerializeField] private int _lifeCost = 1;
    [SerializeField] private SlimeRenderGroup _renderGroup = SlimeRenderGroup.Normal;
    [SerializeField] private int _jellyReward = 1;

    public int SlimeId => _slimeId;
    public string CodeName => _codeName;
    public int BaseHealth => _baseHealth;
    public float BaseSpeed => _baseSpeed;
    public int LifeCost => _lifeCost;
    public SlimeRenderGroup RenderGroup => _renderGroup;
    public int JellyReward => _jellyReward;

    public virtual SlimeKind Kind => SlimeKind.Normal;
    public virtual bool InstantGameOver => false;
}
