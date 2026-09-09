using System;
using UniRx;
using UnityEngine;

public class TowerStats : IDisposable
{
    private readonly ReactiveProperty<float> _attackRange = new ReactiveProperty<float>();
    private readonly ReactiveProperty<float> _attackSpeed = new ReactiveProperty<float>(1f);

    public IReadOnlyReactiveProperty<float> AttackRange => _attackRange;
    public IReadOnlyReactiveProperty<float> AttackSpeed => _attackSpeed;

    // TowerRangeIndicator가 _attackRange를 구독하므로 인스턴스를 교체하면 구독이 끊긴다.
    public void Initialize(TowerData data)
    {
        _attackRange.Value = data.AttackRange;
        _attackSpeed.Value = Mathf.Max(0.01f, data.AttackSpeed);
    }

    public void SetAttackSpeed(float multiplier)
    {
        _attackSpeed.Value = Mathf.Max(0.01f, multiplier);
    }

    public void Dispose()
    {
        _attackRange?.Dispose();
        _attackSpeed?.Dispose();
    }
}
