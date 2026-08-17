using MessagePipe;
using Services.UpdateService;
using VContainer;
using VContainer.Unity;

public class ProjectLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<SceneLoader>(Lifetime.Singleton).As<ISceneLoader>();
        builder.RegisterEntryPoint<BootstrapEntryPoint>();

        builder.RegisterComponentInHierarchy<UpdateSubscriptionService>()
               .As<IUpdateSubscriptionService>();

        var options = builder.RegisterMessagePipe();
        builder.RegisterMessageBroker<SlimeKilledEvent>(options);
    }
}
