using System;
using System.Buffers.Binary;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using SlimeTD.Protocol;
using UnityEditor;
using UnityEngine;

// TODO: 서버 접속 1차 테스트용. 정식 네트워크 계층이 생기면 삭제한다
public static class ServerConnectionTest
{
    private const string ConfigPath = "Assets/Datas/ServerConfig.asset";

    [MenuItem("SlimeTD/Server/Connection Test")]
    private static void Run() => RunAsync().Forget();

    private static async UniTaskVoid RunAsync()
    {
        try
        {
            var config = AssetDatabase.LoadAssetAtPath<ServerConfig>(ConfigPath);
            using var client = new TcpClient { NoDelay = true };
            await client.ConnectAsync(config.TcpHost, config.TcpPort);
            NetworkStream stream = client.GetStream();

            var login = await RequestAsync(stream, MsgId.LoginReq,
                new LoginReq { DeviceId = SystemInfo.deviceUniqueIdentifier }, MsgId.LoginRes, LoginRes.Parser);
            Debug.Log($"[ServerConnectionTest] 로그인: {login.Result}, 계정 {login.AccountId}, 새 계정 {login.IsNewAccount}");
            if (login.Result != ResultCode.Ok)
                return;

            var player = await RequestAsync(stream, MsgId.GetPlayerReq,
                new GetPlayerReq(), MsgId.GetPlayerRes, GetPlayerRes.Parser);
            Debug.Log($"[ServerConnectionTest] 플레이어 데이터: {player.Result}\n{player.Player}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ServerConnectionTest] 실패: {e}");
        }
    }

    private static async UniTask<TResponse> RequestAsync<TResponse>(
        NetworkStream stream, MsgId requestId, IMessage request, MsgId responseId, MessageParser<TResponse> parser)
        where TResponse : IMessage<TResponse>
    {
        byte[] body = request.ToByteArray();
        byte[] frame = new byte[6 + body.Length];
        BinaryPrimitives.WriteUInt32BigEndian(frame, (uint)(2 + body.Length));
        BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(4), (ushort)requestId);
        body.CopyTo(frame, 6);
        await stream.WriteAsync(frame, 0, frame.Length);

        int length = (int)BinaryPrimitives.ReadUInt32BigEndian(await ReadExactAsync(stream, 4));
        byte[] rest = await ReadExactAsync(stream, length);
        ushort id = BinaryPrimitives.ReadUInt16BigEndian(rest);
        if (id != (ushort)responseId)
            throw new InvalidOperationException($"응답 ID가 다릅니다: {id} (기대 {(ushort)responseId})");

        // 응답은 [ID 2][ts 4][rt 4] 뒤가 본문이다
        return parser.ParseFrom(rest, 10, length - 10);
    }

    private static async UniTask<byte[]> ReadExactAsync(NetworkStream stream, int count)
    {
        byte[] buffer = new byte[count];
        int read = 0;
        while (read < count)
        {
            int n = await stream.ReadAsync(buffer, read, count - read);
            if (n == 0)
                throw new InvalidOperationException("서버가 연결을 끊었습니다");
            read += n;
        }
        return buffer;
    }
}
