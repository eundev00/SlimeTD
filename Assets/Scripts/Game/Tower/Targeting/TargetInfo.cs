using UnityEngine;

public readonly struct TargetInfo
{
    public readonly Transform Transform;
    public readonly ISlime Slime;
    public readonly float SqrDistance;
    private readonly int _life;

    public TargetInfo(Transform transform, ISlime slime, float sqrDistance)
    {
        Transform = transform;
        Slime = slime;
        SqrDistance = sqrDistance;
        _life = slime?.Stats != null ? slime.Stats.Life : 0;
    }

    // 죽은 슬라임이 풀에서 바로 다시 나오면 살아 있어도 다른 슬라임이다
    public bool IsValid => Transform != null
        && Transform.gameObject.activeInHierarchy
        && Slime != null
        && !Slime.Stats.IsDead
        && Slime.Stats.Life == _life;
}
