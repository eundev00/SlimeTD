using Services.PoolService;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    [NotNull][SerializeField] private GameConfig _gameConfig;
    [NotNull][SerializeField] private TowerSpawnConfig _towerSpawnConfig;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<GameObjectPoolService>(Lifetime.Scoped)
               .As<IGameObjectPoolService>();

        builder.RegisterInstance(_gameConfig);
        builder.RegisterInstance(_towerSpawnConfig);
        builder.Register<GameplayService>(Lifetime.Scoped).As<IGameplayService>();
        builder.Register<TowerGridService>(Lifetime.Scoped).As<ITowerGridService>();
        builder.Register<TowerSpawner>(Lifetime.Scoped).As<ITowerSpawner>();

        builder.RegisterComponentInHierarchy<WaveSpawner>();
        builder.RegisterComponentInHierarchy<GridMapReference>();
        builder.RegisterComponentInHierarchy<TowerInputHandler>();

        var topHudView = Object.FindFirstObjectByType<TopHudView>();
        if (topHudView != null)
            builder.RegisterComponent(topHudView);

        var gameplayHudView = Object.FindFirstObjectByType<GameplayHudView>();
        if (gameplayHudView != null)
            builder.RegisterComponent(gameplayHudView);

        builder.Register<TopHudPresenter>(Lifetime.Scoped);

        // GameplayService/TopHudPresenter는 구독이 전부라 아무도 주입받지 않으면 생성이 안 되므로 스코프 빌드 시 강제로 생성한다.
        builder.RegisterBuildCallback(container =>
        {
            container.Resolve<IGameplayService>();
            container.Resolve<TopHudPresenter>();
        });
    }
}
