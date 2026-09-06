using UnityEngine;

public class DummySlime : MonoBehaviour, ISlime
{
    [SerializeField] private int _health = 1000;

    private SlimeStats _stats;

    public SlimeStats Stats => _stats;
    public Vector3 Position => transform.position;

    private void Awake()
    {
        _stats = new SlimeStats(_health);
    }

    public void TakeDamage(int damage)
    {
        _stats.TakeDamage(damage);
    }

    private void OnDestroy()
    {
        _stats?.Dispose();
    }
}
