using System;
using UniRx;
using UnityEngine;

public class TowerStats : IDisposable
{
    private readonly ReactiveProperty<float> _attackRange = new ReactiveProperty<float>();
    private readonly ReactiveProperty<float> _attackSpeed = new ReactiveProperty<float>(1f);

    private TowerData _data;

    public IReadOnlyReactiveProperty<float> AttackRange => _attackRange;
    public IReadOnlyReactiveProperty<float> AttackSpeed => _attackSpeed;

    public int Level { get; private set; } = 1;
    public float Attack { get; private set; }
    public float AttackCooldown => _data != null ? _data.AttackCooldown : 1f;
    public float CritChance => _data != null ? _data.CritChance / 100f : 0f;
    public float CritBonus => _data != null ? _data.CritBonus / 100f : 0f;

    // TowerRangeIndicator가 _attackRange를 구독하므로 인스턴스를 교체하면 구독이 끊긴다.
    // 타워 레벨은 로비에서 정해지므로 인게임에서는 생성 시점의 값으로 확정한다.
    public void Initialize(TowerData data, int level)
    {
        _data = data;
        Level = level;
        Attack = data.GetAttack(level);

        _attackRange.Value = data.AttackRange;
        _attackSpeed.Value = Mathf.Max(0.01f, data.AttackSpeedPercent / 100f);
    }

    // 화살(발)마다 한 번씩 호출한다. 다중 발사는 발마다 따로 판정한다.
    public HitResult RollHit()
    {
        float damage = Attack;
        bool critical = UnityEngine.Random.value < CritChance;

        if (critical)
            damage += damage * CritBonus;

        return new HitResult(Mathf.Max(1, Mathf.RoundToInt(damage)), critical);
    }

    public void Dispose()
    {
        _attackRange?.Dispose();
        _attackSpeed?.Dispose();
    }
}