using UniRx;

public class HunterState
{
    public int TowerId { get; }
    public TowerData Data { get; }
    public ReactiveProperty<int> Level { get; }
    public ReactiveProperty<bool> Unlocked { get; }
    public ReactiveProperty<int> Cards { get; }

    public HunterState(TowerData data, HunterRecord record)
    {
        TowerId = data.Id;
        Data = data;
        Level = new ReactiveProperty<int>(record.Level);
        Unlocked = new ReactiveProperty<bool>(record.Unlocked);
        Cards = new ReactiveProperty<int>(record.Cards);
    }

    public void Dispose()
    {
        Level.Dispose();
        Unlocked.Dispose();
        Cards.Dispose();
    }
}
