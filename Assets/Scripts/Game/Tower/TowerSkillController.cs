using System.Collections.Generic;
using UnityEngine;

public class TowerSkillController
{
    private readonly List<TowerSkillData> _onAttackSkills = new List<TowerSkillData>();

    // 처음 맞은 대상 말고 추가로 관통하는 대상 수다 (1이면 총 2마리).
    public int PierceCount { get; }

    public TowerSkillController(IReadOnlyList<TowerSkillData> activeSkills)
    {
        for (int i = 0; i < activeSkills.Count; i++)
        {
            var skill = activeSkills[i];

            if (skill.Trigger == SkillTrigger.OnAttack)
                _onAttackSkills.Add(skill);

            if (skill.Trigger != SkillTrigger.Always)
                continue;

            var effects = skill.Effects;
            for (int e = 0; e < effects.Count; e++)
            {
                if (effects[e].Type == SkillEffectType.Pierce)
                    PierceCount += Mathf.RoundToInt(effects[e].Value);
            }
        }
    }

    // 공격 1회마다 한 번 굴린다. 화살이 여러 발이어도 젤리는 한 번만 얻는다.
    public int RollJellyGain()
    {
        int jelly = 0;

        for (int i = 0; i < _onAttackSkills.Count; i++)
        {
            var skill = _onAttackSkills[i];
            if (skill.Chance > 0f && Random.value * 100f >= skill.Chance)
                continue;

            var effects = skill.Effects;
            for (int e = 0; e < effects.Count; e++)
            {
                if (effects[e].Type == SkillEffectType.JellyGain)
                    jelly += Mathf.RoundToInt(effects[e].Value);
            }
        }

        return jelly;
    }

    // 발동한 스킬이 여럿이면 배율이 가장 큰 것만 쓴다. 반환값은 공격력에 곱하는 배수다(500% -> 5).
    public bool TryRollDamageMultiplier(out float multiplier)
    {
        multiplier = 0f;

        for (int i = 0; i < _onAttackSkills.Count; i++)
        {
            var skill = _onAttackSkills[i];
            if (skill.Chance > 0f && Random.value * 100f >= skill.Chance)
                continue;

            var effects = skill.Effects;
            for (int e = 0; e < effects.Count; e++)
            {
                if (effects[e].Type == SkillEffectType.DamageMultiplier)
                    multiplier = Mathf.Max(multiplier, effects[e].Value / 100f);
            }
        }

        return multiplier > 0f;
    }

    // TODO: OnHit / OnKill / EveryNthAttack 발동과 Slow / Pierce / JellyGain 효과
    public int RollExtraShots()
    {
        int extra = 0;

        for (int i = 0; i < _onAttackSkills.Count; i++)
        {
            var skill = _onAttackSkills[i];
            if (skill.Chance > 0f && Random.value * 100f >= skill.Chance)
                continue;

            var effects = skill.Effects;
            for (int e = 0; e < effects.Count; e++)
            {
                if (effects[e].Type == SkillEffectType.MultiShot)
                    extra += Mathf.RoundToInt(effects[e].Value);
            }
        }

        return extra;
    }
}
