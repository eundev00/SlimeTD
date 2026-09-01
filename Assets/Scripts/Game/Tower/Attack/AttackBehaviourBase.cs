using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public abstract class AttackBehaviourBase : IAttackBehaviour
{
    private readonly AttackBehaviourData _data;
    private float _remainingCooldown;

    protected ITowerContext Context { get; private set; }
    protected AttackBehaviourData Data => _data;

    public virtual bool RequiresFacing => true;
    public bool IsReady => _remainingCooldown <= 0f;

    protected AttackBehaviourBase(AttackBehaviourData data)
    {
        _data = data;
    }

    public virtual void Initialize(ITowerContext context)
    {
        Context = context;
    }

    public void Tick(float deltaTime)
    {
        if (_remainingCooldown > 0f)
            _remainingCooldown -= deltaTime;
    }

    public async UniTask ExecuteAsync(TargetInfo target, CancellationToken token)
    {
        var animator = Context?.Animator;

        // 취소로 중단돼도 차징 상태는 반드시 되돌려야 손에 발사체가 남지 않는다.
        try
        {
            if (_data.ChargeDuration > 0f)
            {
                animator?.Play(_data.ChargeState);
                OnChargeStarted();
                await UniTask.Delay(TimeSpan.FromSeconds(_data.ChargeDuration), cancellationToken: token);
            }

            animator?.Play(_data.AttackState);
            OnChargeEnded();

            // 차징 동안 타겟이 죽거나 풀에 반환됐을 수 있다.
            if (target.IsValid)
            {
                Apply(target);
            }

            if (_data.AttackDuration > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_data.AttackDuration), cancellationToken: token);
            }

            animator?.PlayIdle();
        }
        finally
        {
            // 쿨다운은 공격이 끝난 뒤부터 흐른다. 시작 시점에 걸면 모션 시간과 겹쳐 값이 무의미해진다.
            _remainingCooldown = _data.Cooldown;
            OnChargeEnded();
        }
    }

    protected abstract void Apply(in TargetInfo target);

    protected virtual void OnChargeStarted() { }
    protected virtual void OnChargeEnded() { }

    public virtual void Dispose()
    {
        Context = null;
    }
}
