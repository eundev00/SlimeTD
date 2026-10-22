using MessagePipe;
using Services.UpdateService;
using VContainer;
using VContainer.Unity;

public class ProjectLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<SceneLoader>(Lifetime.Singleton).As<ISceneLoader>();
        builder.Register<ResourceLoadService>(Lifetime.Singleton).As<IResourceLoadService>();
        builder.Register<TowerLevelService>(Lifetime.Singleton).As<ITowerLevelService>();
        builder.Register<LocalPlayerDataStore>(Lifetime.Singleton).As<IPlayerDataStore>();
        builder.Register<PlayerDataService>(Lifetime.Singleton).As<IPlayerDataService>();
        builder.RegisterEntryPoint<BootstrapEntryPoint>();

        builder.RegisterComponentInHierarchy<UpdateSubscriptionService>()
               .As<IUpdateSubscriptionService>();

        var options = builder.RegisterMessagePipe();
        builder.RegisterMessageBroker<SlimeKilledEvent>(options);
        builder.RegisterMessageBroker<SlimeReachedEndEvent>(options);
        builder.RegisterMessageBroker<GameProgressEvent>(options);
        builder.RegisterMessageBroker<TowerSpawnRequestedEvent>(options);
        builder.RegisterMessageBroker<CurrencyViewClickedEvent>(options);
    }
}
