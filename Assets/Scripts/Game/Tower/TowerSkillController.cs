using System.Collections.Generic;
using UnityEngine;

public class TowerSkillController
{
    private readonly List<TowerSkillData> _onAttackSkills = new List<TowerSkillData>();

    public TowerSkillController(IReadOnlyList<TowerSkillData> activeSkills)
    {
        for (int i = 0; i < activeSkills.Count; i++)
        {
            if (activeSkills[i].Trigger == SkillTrigger.OnAttack)
                _onAttackSkills.Add(activeSkills[i]);
        }
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
