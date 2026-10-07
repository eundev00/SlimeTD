using UnityEngine;

[CreateAssetMenu(fileName = "SlimeData_Boss00", menuName = "SlimeTD/Boss Slime Data", order = 41)]
public class BossSlimeData : SlimeDataBase
{
    [Tooltip("켜면 경로 끝 도달 시 즉시 게임오버, 끄면 _lifeCost만큼 차감")]
    [SerializeField] private bool _instantGameOver = true;

    public override bool InstantGameOver => _instantGameOver;
}
