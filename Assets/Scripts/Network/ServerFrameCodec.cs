using System;
using System.Buffers.Binary;
using Google.Protobuf;
using SlimeTD.Protocol;

// 요청: [길이 4][메시지ID 2][본문], 응답: [길이 4][메시지ID 2][ts 4][rt 4][본문]. 정수는 빅엔디언이다
public static class ServerFrameCodec
{
    private const int LengthSize = 4;
    private const int IdSize = 2;
    private const int ResponseExtraSize = 8;

    public static byte[] Encode(MsgId id, IMessage message)
    {
        byte[] body = message.ToByteArray();
        byte[] frame = new byte[LengthSize + IdSize + body.Length];
        BinaryPrimitives.WriteUInt32BigEndian(frame, (uint)(IdSize + body.Length));
        BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(LengthSize), (ushort)id);
        body.CopyTo(frame, LengthSize + IdSize);
        return frame;
    }

    // 서버가 응답 여러 개를 한 번에 보내거나 나눠 보낼 수 있어서 받은 바이트를 모아 두고 완성된 프레임만 꺼낸다
    public class Reader
    {
        private byte[] _buffer = new byte[4096];
        private int _count;

        public void Append(byte[] data, int length)
        {
            if (_count + length > _buffer.Length)
                Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, _count + length));

            Buffer.BlockCopy(data, 0, _buffer, _count, length);
            _count += length;
        }

        // serverTime은 응답 헤더의 ts (서버가 보낸 시각, Unix 초)
        public bool TryRead(out MsgId id, out byte[] body, out uint serverTime)
        {
            id = MsgId.None;
            body = null;
            serverTime = 0;

            if (_count < LengthSize)
                return false;

            int frameLength = (int)BinaryPrimitives.ReadUInt32BigEndian(_buffer.AsSpan(0, LengthSize));
            if (_count < LengthSize + frameLength)
                return false;

            id = (MsgId)BinaryPrimitives.ReadUInt16BigEndian(_buffer.AsSpan(LengthSize, IdSize));
            serverTime = BinaryPrimitives.ReadUInt32BigEndian(_buffer.AsSpan(LengthSize + IdSize, 4));
            int bodyOffset = LengthSize + IdSize + ResponseExtraSize;
            body = _buffer.AsSpan(bodyOffset, frameLength - IdSize - ResponseExtraSize).ToArray();

            int consumed = LengthSize + frameLength;
            Buffer.BlockCopy(_buffer, consumed, _buffer, 0, _count - consumed);
            _count -= consumed;
            return true;
        }

        public void Clear()
        {
            _count = 0;
        }
    }
}