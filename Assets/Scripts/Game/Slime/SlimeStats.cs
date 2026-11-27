using System;
using UniRx;

public class SlimeStats : IDisposable
{
    private ReactiveProperty<int> _currentHealth;

    public IReadOnlyReactiveProperty<int> CurrentHealth => _currentHealth;
    public bool IsDead => _currentHealth.Value <= 0;

    // 풀에서 다시 쓰이면 바뀐다. 예전 생애를 노린 공격을 걸러낸다
    public int Life { get; private set; }

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

    // 협동: 서버가 계산한 남은 체력
    public void SetHealth(int health)
    {
        _currentHealth.Value = Math.Max(0, health);
    }

    public void Reset(int maxHealth)
    {
        _currentHealth.Value = maxHealth;
        Life++;
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
