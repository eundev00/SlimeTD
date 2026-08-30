using Services.PoolService;
using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<GameObjectPoolService>(Lifetime.Scoped)
               .As<IGameObjectPoolService>();

        builder.Register<GameplayService>(Lifetime.Scoped).As<IGameplayService>();
        builder.Register<GroundHeightSampler>(Lifetime.Scoped).As<IGroundHeightSampler>();
        builder.RegisterEntryPoint<GameInitiator>();

        builder.RegisterComponentInHierarchy<Zone>();

        builder.RegisterComponentInHierarchy<TopHudView>();
        builder.RegisterEntryPoint<TopHudPresenter>();

        builder.RegisterComponentInHierarchy<GameplayHudView>();
        builder.RegisterEntryPoint<GameplayHudPresenter>();
    }
}
