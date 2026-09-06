using System.Collections.Generic;
using UnityEngine;

public class PiercingProjectile : Projectile
{
    [SerializeField] private int _pierce = 3;

    private readonly HashSet<BaseSlime> _hitSlimes = new HashSet<BaseSlime>();
    private int _remainingPierce;

    public override void OnGetFromPool()
    {
        base.OnGetFromPool();

        _remainingPierce = Mathf.Max(1, _pierce);
        _hitSlimes.Clear();
    }

    // 풀에 남은 히트 기록을 지우지 않으면 재사용된 발사체가 같은 슬라임을 다시 못 때린다.
    public override void OnReturnToPool()
    {
        base.OnReturnToPool();

        _remainingPierce = 0;
        _hitSlimes.Clear();
    }

    protected override void OnHit(BaseSlime slime)
    {
        if (!_hitSlimes.Add(slime))
            return;

        slime.TakeDamage(Damage);

        _remainingPierce--;
        if (_remainingPierce <= 0)
            ReturnToPool();
    }
}
