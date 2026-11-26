// TODO: 존이 2개 이상이 되면 어느 존 요청인지 구분할 필드를 추가한다
public readonly struct TowerSpawnRequestedEvent
{
    public readonly TowerData TowerData;
    // 0이면 계정 레벨을 쓴다
    public readonly int Level;

    public TowerSpawnRequestedEvent(TowerData towerData, int level = 0)
    {
        TowerData = towerData;
        Level = level;
    }
}
