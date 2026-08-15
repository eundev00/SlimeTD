using VContainer;
using VContainer.Unity;

public class LobbyLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // 이 등록이 없으면 [Inject]가 호출되지 않는다 (씬 전체 자동 주입은 없음)
        builder.RegisterComponentInHierarchy<LobbyStartButton>();
    }
}
