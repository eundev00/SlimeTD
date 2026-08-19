using Services.PoolService;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    [NotNull][SerializeField] private GameConfig _gameConfig;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<GameObjectPoolService>(Lifetime.Scoped)
               .As<IGameObjectPoolService>();

        builder.RegisterInstance(_gameConfig);
        builder.Register<GameplayService>(Lifetime.Scoped).As<IGameplayService>();

        builder.RegisterComponentInHierarchy<WaveSpawner>();
        builder.RegisterComponentInHierarchy<BaseTower>();
        builder.RegisterComponentInHierarchy<HudView>();

        builder.Register<HudViewPresenter>(Lifetime.Scoped);

        // GameplayService/HudViewPresenter는 구독이 전부라 아무도 주입받지 않으면 생성이 안 되므로 스코프 빌드 시 강제로 생성한다.
        builder.RegisterBuildCallback(container =>
        {
            container.Resolve<IGameplayService>();
            container.Resolve<HudViewPresenter>();
        });
    }
}
