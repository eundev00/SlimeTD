using UnityEngine;

public interface IGroundHeightSampler
{
    Vector3 SnapToGround(Vector3 position);
}
