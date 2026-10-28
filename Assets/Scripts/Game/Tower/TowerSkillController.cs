using System.Collections.Generic;
using UnityEngine;

public class TowerSkillController
{
    private readonly List<TowerSkillData> _onAttackSkills = new List<TowerSkillData>();
    private readonly List<float> _nextReadyTimes = new List<float>();

    // 처음 맞은 대상 말고 추가로 관통하는 대상 수다 (1이면 총 2마리).
    public int PierceCount { get; }

    // 한 타워의 감속은 한 줄로만 걸리므로 가장 강한 것 하나를 쓴다.
    public float SlowPercent { get; }
    public float SlowDuration { get; }

    public TowerSkillController(IReadOnlyList<TowerSkillData> activeSkills)
    {
        for (int i = 0; i < activeSkills.Count; i++)
        {
            var skill = activeSkills[i];

            if (skill.Trigger == SkillTrigger.OnAttack)
            {
                _onAttackSkills.Add(skill);
                _nextReadyTimes.Add(0f);
            }

            if (skill.Trigger != SkillTrigger.Always)
                continue;

            var effects = skill.Effects;
            for (int e = 0; e < effects.Count; e++)
            {
                var effect = effects[e];

                if (effect.Type == SkillEffectType.Pierce)
                    PierceCount += Mathf.RoundToInt(effect.Value);

                if (effect.Type == SkillEffectType.Slow && effect.Value > SlowPercent)
                {
                    SlowPercent = effect.Value;
                    SlowDuration = effect.Duration;
                }
            }
        }
    }

    // 공격 1회마다 한 번 굴린다. 화살이 여러 발이어도 발동 판정은 공격당 한 번이다.
    // TODO: OnHit / OnKill / EveryNthAttack 발동
    public AttackSkillRoll RollAttack()
    {
        int extraShots = 0;
        float multiplier = 0f;
        float splashRadius = 0f;
        int jelly = 0;

        for (int i = 0; i < _onAttackSkills.Count; i++)
        {
            if (!TryProc(i))
                continue;

            var effects = _onAttackSkills[i].Effects;
            for (int e = 0; e < effects.Count; e++)
            {
                var effect = effects[e];

                switch (effect.Type)
                {
                    case SkillEffectType.MultiShot:
                        extraShots += Mathf.RoundToInt(effect.Value);
                        break;
                    case SkillEffectType.DamageMultiplier:
                        multiplier = Mathf.Max(multiplier, effect.Value / 100f);
                        break;
                    case SkillEffectType.Splash:
                        splashRadius = Mathf.Max(splashRadius, effect.Value);
                        break;
                    case SkillEffectType.JellyGain:
                        jelly += Mathf.RoundToInt(effect.Value);
                        break;
                }
            }
        }

        return new AttackSkillRoll(extraShots, multiplier, splashRadius, jelly);
    }

    // 쿨타임은 공격속도와 독립이다. 발동했을 때만 시작한다.
    private bool TryProc(int index)
    {
        var skill = _onAttackSkills[index];

        if (skill.Cooldown > 0f && Time.time < _nextReadyTimes[index])
            return false;

        if (skill.Chance > 0f && Random.value * 100f >= skill.Chance)
            return false;

        if (skill.Cooldown > 0f)
            _nextReadyTimes[index] = Time.time + skill.Cooldown;

        return true;
    }
}
