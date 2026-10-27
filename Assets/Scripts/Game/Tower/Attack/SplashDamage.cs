using System.Collections.Generic;
using UnityEngine;

public static class SplashDamage
{
    private const int BufferSize = 32;

    private static readonly Collider[] Buffer = new Collider[BufferSize];
    private static readonly List<BaseSlime> Targets = new List<BaseSlime>(BufferSize);

    // 중심 대상은 이미 맞았으므로 제외하고, 주변 슬라임에게 같은 데미지를 준다.
    public static void Apply(Vector3 center, float radius, DamageInfo info, BaseSlime exclude)
    {
        if (radius <= 0f)
            return;

        int count = Physics.OverlapSphereNonAlloc(center, radius, Buffer, LayerMask.GetMask(GameTags.SlimeLayer));

        // 데미지로 처치되면 순회 중 풀 반환이 일어날 수 있어 먼저 대상을 모은다.
        Targets.Clear();
        for (int i = 0; i < count; i++)
        {
            var slime = Buffer[i].GetComponentInParent<BaseSlime>();
            if (slime == null || slime == exclude || slime.Stats.IsDead || Targets.Contains(slime))
                continue;

            Targets.Add(slime);
        }

        for (int i = 0; i < Targets.Count; i++)
            Targets[i].TakeDamage(info);

        Targets.Clear();
    }
}
