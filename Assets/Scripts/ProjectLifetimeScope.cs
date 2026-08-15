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

        // Bootstrap 씬 하이어라키의 MonoBehaviour를 DI에 등록
        builder.RegisterComponentInHierarchy<UpdateSubscriptionService>()
               .As<IUpdateSubscriptionService>();

        // MessagePipe 초기화 + 이벤트 브로커
        var options = builder.RegisterMessagePipe();
        builder.RegisterMessageBroker<SlimeKilledEvent>(options);
    }
}
