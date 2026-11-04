using UnityEngine;
using VContainer;

public class DummySlime : MonoBehaviour, ISlime
{
    [SerializeField] private int _health = 1000000;
    [SerializeField] private Transform _damageTextAnchor;

    private SlimeStats _stats;
    private DamageTextSpawner _damageTextSpawner;

    public SlimeStats Stats => _stats;
    public Vector3 Position => transform.position;

    [Inject]
    public void Construct(DamageTextSpawner damageTextSpawner)
    {
        _damageTextSpawner = damageTextSpawner;
    }

    private void Awake()
    {
        _stats = new SlimeStats(_health);

        if (_damageTextAnchor == null)
            _damageTextAnchor = transform;
    }

    // 죽지 않는 더미라 체력이 바닥나기 전에 다시 채운다. 0이 되면 SlimeAnimation이 사망 연출을 튼다.
    public void TakeDamage(DamageInfo info)
    {
        if (_stats.CurrentHealth.Value - info.Damage <= 0)
            _stats.Reset(_health);

        _stats.TakeDamage(info.Damage);

        if (_damageTextSpawner != null && info.Damage > 0)
            _damageTextSpawner.Show(info.Damage, _damageTextAnchor.position, info.IsCritical);
    }

    private void OnDestroy()
    {
        _stats?.Dispose();
    }
}
