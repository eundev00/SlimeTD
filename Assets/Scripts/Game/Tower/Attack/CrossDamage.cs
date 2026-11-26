using System.Collections.Generic;
using UnityEngine;

public static class CrossDamage
{
    private const int BufferSize = 32;
    private const float HalfCell = 0.5f;

    private static readonly Collider[] Buffer = new Collider[BufferSize];
    private static readonly List<ISlime> Targets = new List<ISlime>(BufferSize);
    private static readonly List<Vector3> HitCenters = new List<Vector3>(BufferSize);

    // 맞은 대상의 칸과 앞뒤좌우 range칸(한 칸 = 1)의 슬라임에게 같은 데미지를 준다. 맞은 대상도 포함한다
    public static IReadOnlyList<Vector3> Apply(ISlime center, int range, DamageInfo info)
    {
        const float cellSize = GridMapData.FixedCellSize;

        HitCenters.Clear();
        Targets.Clear();

        if (center == null || center.Stats.IsDead)
            return HitCenters;

        Vector3 origin = center.Position;
        float reach = (range + HalfCell) * cellSize;
        float halfWidth = HalfCell * cellSize;

        Targets.Add(center);

        int count = Physics.OverlapSphereNonAlloc(origin, reach * Mathf.Sqrt(2f), Buffer, LayerMask.GetMask(GameTags.SlimeLayer));
        for (int i = 0; i < count; i++)
        {
            var slime = Buffer[i].GetComponentInParent<ISlime>();
            if (slime == null || slime.Stats.IsDead || Targets.Contains(slime))
                continue;

            float dx = Mathf.Abs(slime.Position.x - origin.x);
            float dz = Mathf.Abs(slime.Position.z - origin.z);
            bool inCross = (dx <= halfWidth && dz <= reach) || (dz <= halfWidth && dx <= reach);
            if (inCross)
                Targets.Add(slime);
        }

        // 데미지로 처치되면 풀 반환으로 위치가 바뀌므로 연출 위치를 먼저 모은다
        for (int i = 0; i < Targets.Count; i++)
            HitCenters.Add(GetCenter(Targets[i]));

        for (int i = 0; i < Targets.Count; i++)
            Targets[i].TakeDamage(info);

        Targets.Clear();
        return HitCenters;
    }

    private static Vector3 GetCenter(ISlime slime)
    {
        return slime is Component component && component.TryGetComponent<Collider>(out var collider)
            ? collider.bounds.center
            : slime.Position;
    }
}
