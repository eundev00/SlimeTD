using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class TowerStats : IDisposable
{
    private readonly ReactiveProperty<float> _attackRange = new ReactiveProperty<float>();
    private readonly ReactiveProperty<float> _attackSpeed = new ReactiveProperty<float>(1f);

    private TowerData _data;

    public IReadOnlyReactiveProperty<float> AttackRange => _attackRange;
    public IReadOnlyReactiveProperty<float> AttackSpeed => _attackSpeed;

    private float _critChanceBonus;
    private float _critBonusBonus;

    public int Level { get; private set; } = 1;
    public float Attack { get; private set; }
    public float AttackCooldown => _data != null ? _data.AttackCooldown : 1f;
    public float CritChance => _data != null ? (_data.CritChance + _critChanceBonus) / 100f : 0f;
    public float CritBonus => _data != null ? (_data.CritBonus + _critBonusBonus) / 100f : 0f;

    // TowerRangeIndicator가 _attackRange를 구독하므로 인스턴스를 교체하면 구독이 끊긴다.
    // 타워 레벨은 로비에서 정해지므로 인게임에서는 생성 시점의 값으로 확정한다.
    public void Initialize(TowerData data, int level, IReadOnlyList<TowerSkillData> skills)
    {
        _data = data;
        Level = level;

        float baseAttack = data.GetAttack(level);
        float attackPercent = 0f;
        float attackFixed = 0f;
        float speedPercent = 0f;
        _critChanceBonus = 0f;
        _critBonusBonus = 0f;

        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i].Trigger != SkillTrigger.Always)
                continue;

            var effects = skills[i].Effects;
            for (int e = 0; e < effects.Count; e++)
            {
                var effect = effects[e];
                bool percent = effect.ValueType == SkillValueType.Percent;

                switch (effect.Type)
                {
                    case SkillEffectType.Attack:
                        if (percent) attackPercent += effect.Value;
                        else attackFixed += effect.Value;
                        break;
                    case SkillEffectType.AttackSpeed:
                        speedPercent += effect.Value;
                        break;
                    case SkillEffectType.CritChance:
                        _critChanceBonus += effect.Value;
                        break;
                    case SkillEffectType.CritBonus:
                        _critBonusBonus += effect.Value;
                        break;
                }
            }
        }

        Attack = baseAttack * (1f + attackPercent / 100f) + attackFixed;

        _attackRange.Value = data.AttackRange;
        _attackSpeed.Value = Mathf.Max(0.01f, data.AttackSpeedPercent / 100f * (1f + speedPercent / 100f));
    }

    public DamageInfo RollHit()
    {
        float damage = Attack;
        bool critical = UnityEngine.Random.value < CritChance;

        if (critical)
            damage += damage * CritBonus;

        return new DamageInfo(Mathf.Max(1, Mathf.RoundToInt(damage)), critical);
    }

    // 스킬 배율 공격은 치명타 판정을 하지 않는다.
    public DamageInfo RollSkillHit(float multiplier)
    {
        return new DamageInfo(Mathf.Max(1, Mathf.RoundToInt(Attack * multiplier)), false);
    }

    public void Dispose()
    {
        _attackRange?.Dispose();
        _attackSpeed?.Dispose();
    }
}