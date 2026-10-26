using UniRx;

// 게임플레이 진행 수치(라이프/젤리 등)를 담는 상태 컨테이너. 로직은 갖지 않고 값 저장/노출만 한다.
// 값 변경은 전부 GameplayService를 통해서만 일어난다. 수치가 늘어나면 여기에 필드만 추가한다.
public class GameplayInfo
{
    public ReactiveProperty<int> Life { get; }
    public ReactiveProperty<int> Jelly { get; }
    public ReactiveProperty<int> CurrentWave { get; }
    public ReactiveProperty<int> MaxWave { get; }
    public ReactiveProperty<int> ReachedWave { get; }
    public ReactiveProperty<int> PreviousBestWave { get; }
    public ReactiveProperty<int> SummonCost { get; }
    public ReactiveProperty<bool> IsCleared { get; }

    public GameplayInfo(int startingLife, int startingJelly)
    {
        Life = new ReactiveProperty<int>(startingLife);
        Jelly = new ReactiveProperty<int>(startingJelly);
        CurrentWave = new ReactiveProperty<int>(0);
        MaxWave = new ReactiveProperty<int>(0);
        ReachedWave = new ReactiveProperty<int>(0);
        PreviousBestWave = new ReactiveProperty<int>(0);
        SummonCost = new ReactiveProperty<int>(0);
        IsCleared = new ReactiveProperty<bool>(false);
    }

    public void Dispose()
    {
        Life.Dispose();
        Jelly.Dispose();
        CurrentWave.Dispose();
        MaxWave.Dispose();
        ReachedWave.Dispose();
        PreviousBestWave.Dispose();
        SummonCost.Dispose();
        IsCleared.Dispose();
    }
}
