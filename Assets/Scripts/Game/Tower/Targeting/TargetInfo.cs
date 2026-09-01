using UnityEngine;

public readonly struct TargetInfo
{
    public readonly Transform Transform;
    public readonly BaseSlime Slime;
    public readonly float SqrDistance;

    public TargetInfo(Transform transform, BaseSlime slime, float sqrDistance)
    {
        Transform = transform;
        Slime = slime;
        SqrDistance = sqrDistance;
    }

    public bool IsValid => Transform != null && Transform.gameObject.activeInHierarchy;
}
