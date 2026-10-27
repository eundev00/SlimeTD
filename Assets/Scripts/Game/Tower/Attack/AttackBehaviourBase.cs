using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public abstract class AttackBehaviourBase : IAttackBehaviour
{
    private const float EndEventTimeout = 10f;
    private const string AttackTrigger = "Attack";

    private float _remainingCooldown;

    private TargetInfo _pendingTarget;
    private bool _hasPendingTarget;
    private UniTaskCompletionSource _endSource;

    protected ITowerContext Context { get; private set; }

    public virtual bool RequiresFacing => true;
    public bool IsReady => _remainingCooldown <= 0f;
    public bool IsAiming { get; private set; }

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
        var animator = Context.Animator;
        var stats = Context.Stats;
        string attackState = AttackTrigger;

        _pendingTarget = target;
        _hasPendingTarget = true;
        _endSource = new UniTaskCompletionSource();

        IsAiming = true;

        try
        {
            OnAttackStarted();

            animator.SetTrigger(attackState);

            // 클립의 OnAttackEnd 이벤트가 완료시킨다. 이벤트가 없으면 타임아웃까지 대기한다.
            TimeoutEndSourceAsync(_endSource, token).Forget();

            using (token.Register(() => _endSource.TrySetCanceled()))
            {
                await _endSource.Task;
            }
        }
        finally
        {
            animator.ResetTrigger(attackState);

            IsAiming = false;
            _hasPendingTarget = false;
            _pendingTarget = default;
            _endSource = null;

            OnAttackFinished();

            // 쿨다운은 공격이 끝난 뒤부터 흐른다. 시작 시점에 걸면 모션 시간과 겹쳐 값이 무의미해진다.
            _remainingCooldown = stats.AttackCooldown / stats.AttackSpeed.Value;
        }
    }

    public void OnAnimationCast()
    {
        OnCastFrame();
    }

    public void OnAnimationHit()
    {
        IsAiming = false;

        if (!_hasPendingTarget)
            return;

        var target = _pendingTarget;
        _hasPendingTarget = false;

        OnHitFrame();

        if (!target.IsValid)
            return;

        var roll = Context.Skills != null ? Context.Skills.RollAttack() : default;
        Apply(target, roll);

        if (roll.Jelly > 0)
            Context.AddJelly(roll.Jelly);
    }

    public void OnAnimationEnd()
    {
        _endSource?.TrySetResult();
    }

    private async UniTaskVoid TimeoutEndSourceAsync(UniTaskCompletionSource source, CancellationToken token)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(EndEventTimeout), cancellationToken: token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (source.TrySetResult())
            Debug.Log($"[{GetType().Name}] 공격 종료 이벤트가 오지 않았습니다. 클립에 OnAttackEnd가 있는지 확인하세요.");
    }

    protected abstract void Apply(in TargetInfo target, in AttackSkillRoll roll);

    // 데미지 스킬이 발동한 공격은 치명타 없이 배율로만 계산하고, 아니면 치명타를 굴린다.
    protected DamageInfo ResolveDamage(in AttackSkillRoll roll)
    {
        if (!roll.HasDamageProc)
            return Context.Stats.RollHit();

        return Context.Stats.RollSkillHit(roll.DamageMultiplier > 0f ? roll.DamageMultiplier : 1f);
    }

    protected virtual void OnAttackStarted() { }
    protected virtual void OnCastFrame() { }
    protected virtual void OnHitFrame() { }
    protected virtual void OnAttackFinished() { }

    public virtual void Dispose()
    {
        _endSource?.TrySetCanceled();
        _endSource = null;
        Context = null;
    }
}
