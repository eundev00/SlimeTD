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
        // 오프라인 테스트 브랜치: 서버에 접속하지 않고 실행마다 새 계정으로 시작한다
        builder.Register<OfflineAccountService>(Lifetime.Singleton).As<IAccountService>();
        builder.Register<OfflineSingleBattleService>(Lifetime.Singleton).As<ISingleBattleService>();
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
