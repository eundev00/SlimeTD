using System;
using UniRx;

public class TowerStats : IDisposable
{
    private readonly ReactiveProperty<float> _attackRange = new ReactiveProperty<float>();

    public IReadOnlyReactiveProperty<float> AttackRange => _attackRange;

    // TowerRangeIndicator가 _attackRange를 구독하므로 인스턴스를 교체하면 구독이 끊긴다.
    public void Initialize(TowerData data)
    {
        _attackRange.Value = data.AttackRange;
    }

    public void Dispose()
    {
        _attackRange?.Dispose();
    }
}
