using UnityEngine;

public abstract class SlimeDataBase : ScriptableObject
{
    [SerializeField] private GameObject _prefab;
    [SerializeField] private int _baseHealth = 3;
    [SerializeField] private float _baseSpeed = 1f;
    [SerializeField] private int _lifeCost = 1;
    [SerializeField] private SlimeRenderGroup _renderGroup = SlimeRenderGroup.Normal;

    public GameObject Prefab => _prefab;
    public int BaseHealth => _baseHealth;
    public float BaseSpeed => _baseSpeed;
    public int LifeCost => _lifeCost;
    public SlimeRenderGroup RenderGroup => _renderGroup;
}
