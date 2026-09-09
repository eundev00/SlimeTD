using Cysharp.Threading.Tasks;
using System;
using System.Threading;

public interface IAttackBehaviour : IDisposable
{
    bool RequiresFacing { get; }
    bool IsReady { get; }
    bool IsAiming { get; }

    void Initialize(ITowerContext context);
    void Tick(float deltaTime);
    UniTask ExecuteAsync(TargetInfo target, CancellationToken token);

    void OnAnimationHit();
    void OnAnimationEnd();
}
