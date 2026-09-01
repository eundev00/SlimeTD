using UnityEngine;

[CreateAssetMenu(fileName = "SlimeData_00", menuName = "SlimeTD/Slime Data", order = 40)]
public class SlimeData : ScriptableObject
{
    [SerializeField] private GameObject _prefab;
    [SerializeField] private int _baseHealth = 3;
    [SerializeField] private float _baseSpeed = 1f;
    [SerializeField] private int _lifeCost = 1;
    [SerializeField] private int _goldReward = 1;

    public GameObject Prefab => _prefab;
    public int BaseHealth => _baseHealth;
    public float BaseSpeed => _baseSpeed;
    public int LifeCost => _lifeCost;
    public int GoldReward => _goldReward;
}
