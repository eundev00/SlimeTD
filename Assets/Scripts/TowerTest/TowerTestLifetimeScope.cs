using MessagePipe;
using Services.PoolService;
using Services.UpdateService;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class TowerTestLifetimeScope : LifetimeScope
{
    [SerializeField] private RectTransform _popupRoot;

    // Bootstrap 없이 바로 실행하려고 부모 스코프 없이 ProjectLifetimeScope의 전역 서비스까지 직접 등록한다.
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<ResourceLoadService>(Lifetime.Singleton).As<IResourceLoadService>();
        builder.Register<TowerLevelService>(Lifetime.Singleton).As<ITowerLevelService>();
        builder.Register<LocalPlayerDataStore>(Lifetime.Singleton).As<IPlayerDataStore>();
        builder.Register<PlayerDataService>(Lifetime.Singleton).As<IPlayerDataService>();

        builder.RegisterComponentInHierarchy<UpdateSubscriptionService>()
               .As<IUpdateSubscriptionService>();

        var options = builder.RegisterMessagePipe();
        builder.RegisterMessageBroker<SlimeKilledEvent>(options);
        builder.RegisterMessageBroker<SlimeReachedEndEvent>(options);
        builder.RegisterMessageBroker<GameProgressEvent>(options);
        builder.RegisterMessageBroker<TowerSpawnRequestedEvent>(options);
        builder.RegisterMessageBroker<CurrencyViewClickedEvent>(options);

        builder.Register<GameObjectPoolService>(Lifetime.Scoped).As<IGameObjectPoolService>();
        builder.Register<GameplayService>(Lifetime.Singleton).As<IGameplayService>();
        builder.Register<TowerPrefabProvider>(Lifetime.Singleton).As<ITowerPrefabProvider>();

        builder.RegisterInstance(_popupRoot);
        builder.RegisterEntryPoint<PopupService>();

        builder.RegisterComponentInHierarchy<DamageTextSpawner>();
        builder.RegisterComponentInHierarchy<TowerTestSpawner>();
        builder.RegisterEntryPoint<TowerTestInitiator>();

        builder.RegisterBuildCallback(resolver =>
        {
            foreach (var dummy in FindObjectsByType<DummySlime>(FindObjectsSortMode.None))
                resolver.Inject(dummy);
        });
    }
}
