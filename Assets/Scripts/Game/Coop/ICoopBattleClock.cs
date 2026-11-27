// 협동 화면이 그리는 서버 틱 (소수). 서버 시간보다 조금 늦게 따라간다
public interface ICoopBattleClock
{
    float Tick { get; }
}
