using System;
using UniRx;

public class SlimeStats : IDisposable
{
    private ReactiveProperty<int> _currentHealth;

    public IReadOnlyReactiveProperty<int> CurrentHealth => _currentHealth;
    public bool IsDead => _currentHealth.Value <= 0;

    public SlimeStats(int maxHealth)
    {
        _currentHealth = new ReactiveProperty<int>(maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (_currentHealth.Value <= 0)
            return;

        _currentHealth.Value -= damage;
    }

    public void Reset(int maxHealth)
    {
        _currentHealth.Value = maxHealth;
    }

    public void Kill()
    {
        if (_currentHealth.Value <= 0)
            return;

        _currentHealth.Value = 0;
    }

    public void Dispose()
    {
        _currentHealth?.Dispose();
    }
}
