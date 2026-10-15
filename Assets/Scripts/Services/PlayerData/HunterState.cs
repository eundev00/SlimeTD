using UniRx;

public class HunterState
{
    public int TowerId { get; }
    public TowerData Data { get; }
    public ReactiveProperty<int> Level { get; }
    public ReactiveProperty<int> Cards { get; }
    public ReactiveProperty<bool> Unlocked { get; }

    public HunterState(TowerData data, HunterRecord record)
    {
        TowerId = data.Id;
        Data = data;
        Level = new ReactiveProperty<int>(record.Level);
        Cards = new ReactiveProperty<int>(record.Cards);
        Unlocked = new ReactiveProperty<bool>(record.Unlocked);
    }

    public void Dispose()
    {
        Level.Dispose();
        Cards.Dispose();
        Unlocked.Dispose();
    }
}