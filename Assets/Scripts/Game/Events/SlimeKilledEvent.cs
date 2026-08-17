using UnityEngine;

public struct SlimeKilledEvent
{
    public readonly int SlimeInstanceId;
    public readonly Vector3 Position;

    public SlimeKilledEvent(int slimeInstanceId, Vector3 position)
    {
        SlimeInstanceId = slimeInstanceId;
        Position = position;
    }
}
