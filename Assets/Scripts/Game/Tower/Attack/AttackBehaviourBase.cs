using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public abstract class AttackBehaviourBase : IAttackBehaviour
{
    private const float EndEventTimeout = 10f;
    private const string AttackTrigger = "Attack";

    private float _remainingCooldown;

    private TargetInfo _pendingTarget;
    private bool _hasPendingTarget;
    private AttackSkillRoll _pendingRoll;
    private DamageInfo _pendingDamage;
    private bool _vfxPending;
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

        _pendingTarget = target;
        _hasPendingTarget = true;
        _endSource = new UniTaskCompletionSource();

        // 스킬과 치명타는 공격 시작에 정한다. 스킬마다 공격 모션이 달라서 트리거를 보내기 전에 결과를 알아야 한다.
        var skills = Context.Skills;
        _pendingRoll = skills != null ? skills.RollAttack() : default;
        _pendingDamage = ResolveDamage(_pendingRoll);
        _vfxPending = skills != null && skills.LastProcs.Count > 0;
        string attackTrigger = SelectTrigger(skills, animator);

        IsAiming = true;

        try
        {
            OnAttackStarted();

            animator.SetTrigger(attackTrigger);

            // 클립의 OnAttackEnd 이벤트가 완료시킨다. 이벤트가 없으면 타임아웃까지 대기한다.
            TimeoutEndSourceAsync(_endSource, token).Forget();

            using (token.Register(() => _endSource.TrySetCanceled()))
            {
                await _endSource.Task;
            }
        }
        finally
        {
            animator.ResetTrigger(attackTrigger);

            IsAiming = false;
            _hasPendingTarget = false;
            _pendingTarget = default;
            _pendingRoll = default;
            _pendingDamage = default;
            _vfxPending = false;
            _endSource = null;

            OnAttackFinished();

            // 쿨다운은 공격이 끝난 뒤부터 흐른다. 시작 시점에 걸면 모션 시간과 겹쳐 값이 무의미해진다.
            _remainingCooldown = stats.AttackCooldown / stats.AttackSpeed.Value;
        }
    }

    // 발동한 스킬 중 전용 모션이 있는 첫 스킬의 트리거를 쓴다. 애니메이터에 없으면 기본 공격으로 대신한다.
    private string SelectTrigger(TowerSkillController skills, TowerAnimator animator)
    {
        if (skills == null)
            return AttackTrigger;

        IReadOnlyList<TowerSkillData> procs = skills.LastProcs;
        for (int i = 0; i < procs.Count; i++)
        {
            string trigger = procs[i].AnimTrigger;
            if (string.IsNullOrEmpty(trigger))
                continue;

            if (animator.HasParameter(trigger))
                return trigger;

            Debug.Log($"[{GetType().Name}] 애니메이터에 트리거 {trigger}가 없어 기본 공격으로 대신합니다. 스킬: {procs[i].Id}", animator);
        }

        return AttackTrigger;
    }

    public void OnAnimationCast()
    {
        if (_hasPendingTarget)
            PlayPendingVfx();

        OnCastFrame();
    }

    public void OnAnimationHit()
    {
        IsAiming = false;

        if (!_hasPendingTarget)
            return;

        var target = _pendingTarget;
        _hasPendingTarget = false;

        // Cast 이벤트가 없는 클립은 타격 때 연출을 띄운다.
        PlayPendingVfx();
        OnHitFrame();

        if (!target.IsValid)
            return;

        var roll = _pendingRoll;
        Apply(target, roll, _pendingDamage);

        if (roll.Jelly > 0)
            Context.AddJelly(roll.Jelly);
    }

    private void PlayPendingVfx()
    {
        if (!_vfxPending)
            return;

        _vfxPending = false;
        Context.PlaySkillVfx(Context.Skills.LastProcs);
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

    protected abstract void Apply(in TargetInfo target, in AttackSkillRoll roll, in DamageInfo info);

    // 데미지 스킬이 발동한 공격은 치명타 없이 배율로만 계산하고, 아니면 치명타를 굴린다.
    private DamageInfo ResolveDamage(in AttackSkillRoll roll)
    {
        var info = roll.HasDamageProc
            ? Context.Stats.RollSkillHit(roll.DamageMultiplier > 0f ? roll.DamageMultiplier : 1f)
            : Context.Stats.RollHit();

        var skills = Context.Skills;
        if (skills != null && skills.SlowPercent > 0f)
            info = info.WithSlow(Context.SpawnId, skills.SlowPercent, skills.SlowDuration);

        if (roll.StunDuration > 0f)
            info = info.WithStun(roll.StunDuration);

        return info;
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
