using UnityEngine;

public class GroundHeightSampler : IGroundHeightSampler
{
    private const float StartHeight = 100f;
    private const float MaxDistance = 200f;

    private readonly LayerMask _groundLayer = LayerMask.GetMask(GameTags.GroundLayer);

    public Vector3 SnapToGround(Vector3 position)
    {
        var rayOrigin = new Vector3(position.x, position.y + StartHeight, position.z);

        if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, MaxDistance, _groundLayer))
        {
            return hit.point;
        }

        return position;
    }
}
