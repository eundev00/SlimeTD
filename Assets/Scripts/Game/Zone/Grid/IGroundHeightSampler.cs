using UnityEngine;

public interface IGroundHeightSampler
{
    Vector3 SnapToGround(Vector3 position);
    bool TryGetGroundHeight(Vector3 position, out float height);
}
