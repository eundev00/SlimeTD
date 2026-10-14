using UnityEngine;

public abstract class SlimeDataBase : ScriptableObject
{
    [SerializeField] private GameObject _prefab;
    [Tooltip("일반: 웨이브 성장률 적용 전 기본값 / 보스: 이 값 그대로 사용")]
    [SerializeField] private int _baseHealth = 3;
    [SerializeField] private float _baseSpeed = 1f;
    [SerializeField] private int _lifeCost = 1;
    [SerializeField] private SlimeRenderGroup _renderGroup = SlimeRenderGroup.Normal;
    [SerializeField] private int _goldReward = 1;

    public GameObject Prefab => _prefab;
    public int BaseHealth => _baseHealth;
    public float BaseSpeed => _baseSpeed;
    public int LifeCost => _lifeCost;
    public SlimeRenderGroup RenderGroup => _renderGroup;
    public int GoldReward => _goldReward;

    public virtual bool InstantGameOver => false;
}
