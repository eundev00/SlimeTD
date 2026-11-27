using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public abstract class AttackBehaviourBase : IAttackBehaviour
{
    private const float HitEventTimeout = 10f;
    private const string AttackTrigger = "Attack";

    private float _remainingCooldown;

    private TargetInfo _pendingTarget;
    private bool _hasPendingTarget;
    private AttackSkillRoll _pendingRoll;
    private DamageInfo _pendingDamage;
    private bool _vfxPending;
    private UniTaskCompletionSource _hitSource;

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

    public void DelayNextAttack(float seconds)
    {
        _remainingCooldown = Mathf.Max(_remainingCooldown, seconds);
    }

    public async UniTask ExecuteAsync(TargetInfo target, CancellationToken token)
    {
        var animator = Context.Animator;
        var stats = Context.Stats;

        // 간격은 공격 시작부터 다음 공격 시작까지다 (서버와 같은 규칙)
        _remainingCooldown = stats.AttackInterval / stats.AttackSpeed.Value;

        _pendingTarget = target;
        _hasPendingTarget = true;
        _hitSource = new UniTaskCompletionSource();

        // 스킬과 치명타는 공격 시작에 정한다. 스킬마다 공격 모션이 달라서 트리거를 보내기 전에 결과를 알아야 한다.
        var skills = Context.Skills;
        _pendingRoll = skills != null ? skills.RollAttack() : default;
        _pendingDamage = ResolveDamage(_pendingRoll);

        // 골드 스킬은 타격 결과와 상관없이 발동하면 바로 준다 (서버 검증 규칙)
        if (_pendingRoll.Gold > 0)
            Context.AddGold(_pendingRoll.Gold);
        _vfxPending = skills != null && (skills.LastProcs.Count > 0 || skills.AttackVfxSkills.Count > 0);
        string attackTrigger = SelectTrigger(skills, animator);

        IsAiming = true;

        try
        {
            OnAttackStarted();

            animator.SetTrigger(attackTrigger);

            // 공격은 타격 프레임에서 끝난다. 남은 모션은 다음 공격 트리거가 끊는다 (모션이 간격보다 긴 궁수)
            TimeoutHitSourceAsync(_hitSource, token).Forget();

            using (token.Register(() => _hitSource.TrySetCanceled()))
            {
                await _hitSource.Task;
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
            _hitSource = null;

            OnAttackFinished();
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

        if (target.IsValid)
        {
            // 십자 범위 스킬은 공격 방식과 상관없이 투사체 없이 타격 순간 바로 들어간다
            if (_pendingRoll.CrossRange > 0)
            {
                var hitCenters = CrossDamage.Apply(target.Slime, _pendingRoll.CrossRange, _pendingDamage);
                for (int i = 0; i < hitCenters.Count; i++)
                    Context.PlayTargetVfx(Context.Skills.LastProcs, hitCenters[i]);
            }
            else
            {
                Apply(target, _pendingRoll, _pendingDamage);
            }
        }

        _hitSource?.TrySetResult();
    }

    private void PlayPendingVfx()
    {
        if (!_vfxPending)
            return;

        _vfxPending = false;

        var skills = Context.Skills;
        Context.PlaySkillVfx(skills.AttackVfxSkills);
        Context.PlaySkillVfx(skills.LastProcs);
    }

    private async UniTaskVoid TimeoutHitSourceAsync(UniTaskCompletionSource source, CancellationToken token)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(HitEventTimeout), cancellationToken: token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (source.TrySetResult())
            Debug.Log($"[{GetType().Name}] 타격 이벤트가 오지 않았습니다. 클립에 OnAttackHit가 있는지 확인하세요.");
    }

    protected abstract void Apply(in TargetInfo target, in AttackSkillRoll roll, in DamageInfo info);

    // 스킬이 발동한 공격은 치명타 없이 배율로만 계산하고, 아니면 치명타를 굴린다.
    private DamageInfo ResolveDamage(in AttackSkillRoll roll)
    {
        var info = roll.HasProc
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
        _hitSource?.TrySetCanceled();
        _hitSource = null;
        Context = null;
    }
}
