using Cysharp.Threading.Tasks;
using System;
using System.Threading;

public interface IAttackBehaviour : IDisposable
{
    bool RequiresFacing { get; }
    bool IsReady { get; }

    void Initialize(ITowerContext context);
    void Tick(float deltaTime);
    UniTask ExecuteAsync(TargetInfo target, CancellationToken token);
}
