using UnityEngine;

/// 슬라임 처치 시 발행되는 MessagePipe 이벤트
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
