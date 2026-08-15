using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // 오브젝트 풀 서비스 (게임 씬 스코프 — 씬 언로드 시 Dispose)
        builder.Register<GameObjectPoolService>(Lifetime.Scoped);

        // 씬 내 MonoBehaviour 주입
        builder.RegisterComponentInHierarchy<TestSpawner>();
        builder.RegisterComponentInHierarchy<Tower>();
    }
}
