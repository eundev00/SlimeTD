using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<GameObjectPoolService>(Lifetime.Scoped);
        builder.RegisterComponentInHierarchy<TestSpawner>();
        builder.RegisterComponentInHierarchy<Tower>();
    }
}
