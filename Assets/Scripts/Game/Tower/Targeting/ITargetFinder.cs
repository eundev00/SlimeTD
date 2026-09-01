using UnityEngine;

public interface ITargetFinder
{
    bool TryFind(Vector3 origin, float range, out TargetInfo target);
}
