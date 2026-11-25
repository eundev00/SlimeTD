using UnityEngine;

public class GroundHeightSampler : IGroundHeightSampler
{
    private const float StartHeight = 100f;
    private const float MaxDistance = 200f;

    private readonly LayerMask _groundLayer = LayerMask.GetMask(GameTags.GroundLayer);

    public Vector3 SnapToGround(Vector3 position)
    {
        return TryGetGroundHeight(position, out float height)
            ? new Vector3(position.x, height, position.z)
            : position;
    }

    public bool TryGetGroundHeight(Vector3 position, out float height)
    {
        var rayOrigin = new Vector3(position.x, position.y + StartHeight, position.z);

        if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, MaxDistance, _groundLayer))
        {
            height = hit.point.y;
            return true;
        }

        height = position.y;
        return false;
    }
}
