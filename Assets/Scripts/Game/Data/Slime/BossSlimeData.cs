using UnityEngine;

[CreateAssetMenu(fileName = "SlimeData_Boss00", menuName = "SlimeTD/Boss Slime Data", order = 41)]
public class BossSlimeData : SlimeDataBase
{
    [SerializeField] private int _goldReward = 100;

    public int GoldReward => _goldReward;
}
