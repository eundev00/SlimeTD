using Services.PoolService;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    [SerializeField] private RectTransform _popupRoot;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<GameObjectPoolService>(Lifetime.Scoped)
               .As<IGameObjectPoolService>();

        // 팝업 Presenter는 PopupService의 자식 스코프에서 해결되므로 Scoped면 게임과 다른 인스턴스를 받는다
        builder.Register<GameplayService>(Lifetime.Singleton).As<IGameplayService>();
        builder.Register<GroundHeightSampler>(Lifetime.Scoped).As<IGroundHeightSampler>();
        builder.Register<SpawnOrderCounter>(Lifetime.Scoped).As<ISpawnOrderCounter>();
        builder.RegisterEntryPoint<GameInitiator>();

        builder.RegisterComponentInHierarchy<Zone>();

        builder.RegisterComponentInHierarchy<TopHudView>();
        builder.RegisterEntryPoint<TopHudPresenter>();

        builder.RegisterComponentInHierarchy<GameplayHudView>();
        builder.RegisterEntryPoint<GameplayHudPresenter>();

        builder.RegisterInstance(_popupRoot);
        builder.RegisterEntryPoint<PopupService>();

        builder.RegisterComponentInHierarchy<TowerActionView>();
        builder.RegisterEntryPoint<TowerActionPresenter>();

        builder.RegisterComponentInHierarchy<DamageTextSpawner>();
    }
}
