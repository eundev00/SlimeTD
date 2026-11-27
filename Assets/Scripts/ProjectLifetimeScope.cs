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
        builder.Register<GameServerConnection>(Lifetime.Singleton).As<IGameServerConnection>();
        builder.Register<AccountService>(Lifetime.Singleton).As<IAccountService>();
        builder.Register<SingleBattleService>(Lifetime.Singleton).As<ISingleBattleService>();
        builder.Register<CoopBattleService>(Lifetime.Singleton).As<ICoopBattleService>();
        builder.Register<CheatService>(Lifetime.Singleton).As<ICheatService>();
        builder.RegisterEntryPoint<BootstrapEntryPoint>();
        builder.RegisterComponentInHierarchy<ServerConnectErrorView>();

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
