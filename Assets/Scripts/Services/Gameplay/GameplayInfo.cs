using UniRx;

// 게임플레이 진행 수치(라이프/골드 등)를 담는 상태 컨테이너. 로직은 갖지 않고 값 저장/노출만 한다.
// 값 변경은 전부 GameplayService를 통해서만 일어난다. 수치가 늘어나면 여기에 필드만 추가한다.
public class GameplayInfo
{
    public ReactiveProperty<int> Life { get; }
    public ReactiveProperty<int> Gold { get; }

    public GameplayInfo(int startingLife, int startingGold)
    {
        Life = new ReactiveProperty<int>(startingLife);
        Gold = new ReactiveProperty<int>(startingGold);
    }

    public void Dispose()
    {
        Life.Dispose();
        Gold.Dispose();
    }
}
