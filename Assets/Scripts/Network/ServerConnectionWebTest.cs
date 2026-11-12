#if UNITY_WEBGL && !UNITY_EDITOR && SLIMETD_WEB_DEBUG
using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using AOT;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using SlimeTD.Protocol;
using UnityEngine;

// TODO: 서버 접속 2차 테스트용(WebGL WebSocket). 정식 네트워크 계층이 생기면 삭제한다
public static class ServerConnectionWebTest
{
    private const string DeviceIdKey = "SlimeTD.DeviceId";
    private const string ServerParameter = "server";

    private delegate void OpenCallback(int socket);
    private delegate void MessageCallback(int socket, IntPtr data, int length);
    private delegate void CloseCallback(int socket, int code);

    [DllImport("__Internal")]
    private static extern int SlimeTD_WsConnect(string url, OpenCallback onOpen, MessageCallback onMessage, CloseCallback onClose);

    [DllImport("__Internal")]
    private static extern int SlimeTD_WsSend(int socket, byte[] data, int length);

    [DllImport("__Internal")]
    private static extern void SlimeTD_WsClose(int socket);

    private static UniTaskCompletionSource _open;
    private static UniTaskCompletionSource<(ushort Id, byte[] Body)> _response;
    private static byte[] _buffer = new byte[4096];
    private static int _count;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Run() => RunAsync().Forget();

    private static async UniTaskVoid RunAsync()
    {
        int socket = 0;
        try
        {
            string url = GetQueryParameter(Application.absoluteURL, ServerParameter);
            if (string.IsNullOrEmpty(url))
            {
                Debug.Log($"[ServerConnectionTest] 페이지 주소에 {ServerParameter} 파라미터가 없어 접속 테스트를 건너뜁니다.");
                return;
            }

            Debug.Log($"[ServerConnectionTest] 접속: {url}");

            _open = new UniTaskCompletionSource();
            socket = SlimeTD_WsConnect(url, OnOpen, OnMessage, OnClose);
            await _open.Task;

            var login = await RequestAsync(socket, MsgId.LoginReq,
                new LoginReq { DeviceId = GetDeviceId() }, MsgId.LoginRes, LoginRes.Parser);
            Debug.Log($"[ServerConnectionTest] 로그인: {login.Result}, 계정 {login.AccountId}, 새 계정 {login.IsNewAccount}");
            if (login.Result != ResultCode.Ok)
                return;

            var player = await RequestAsync(socket, MsgId.GetPlayerReq,
                new GetPlayerReq(), MsgId.GetPlayerRes, GetPlayerRes.Parser);
            Debug.Log($"[ServerConnectionTest] 플레이어 데이터: {player.Result}\n{player.Player}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ServerConnectionTest] 실패: {e}");
        }
        finally
        {
            if (socket != 0)
                SlimeTD_WsClose(socket);
        }
    }

    // 터널 주소가 바뀌어도 다시 빌드하지 않도록 서버 주소는 페이지 URL의 ?server=wss://…/ws로 받는다
    private static string GetQueryParameter(string pageUrl, string name)
    {
        int start = pageUrl.IndexOf('?');
        if (start < 0)
            return null;

        int end = pageUrl.IndexOf('#', start);
        string query = end < 0 ? pageUrl.Substring(start + 1) : pageUrl.Substring(start + 1, end - start - 1);

        foreach (string pair in query.Split('&'))
        {
            int separator = pair.IndexOf('=');
            if (separator > 0 && pair.Substring(0, separator) == name)
                return Uri.UnescapeDataString(pair.Substring(separator + 1));
        }

        return null;
    }

    // WebGL은 deviceUniqueIdentifier가 비거나 매번 달라질 수 있어 처음 만든 GUID를 저장해 쓴다
    private static string GetDeviceId()
    {
        string id = PlayerPrefs.GetString(DeviceIdKey, string.Empty);
        if (!string.IsNullOrEmpty(id))
            return id;

        id = Guid.NewGuid().ToString("N");
        PlayerPrefs.SetString(DeviceIdKey, id);
        PlayerPrefs.Save();
        return id;
    }

    private static async UniTask<TResponse> RequestAsync<TResponse>(
        int socket, MsgId requestId, IMessage request, MsgId responseId, MessageParser<TResponse> parser)
        where TResponse : IMessage<TResponse>
    {
        byte[] body = request.ToByteArray();
        byte[] frame = new byte[6 + body.Length];
        BinaryPrimitives.WriteUInt32BigEndian(frame, (uint)(2 + body.Length));
        BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(4), (ushort)requestId);
        body.CopyTo(frame, 6);

        _response = new UniTaskCompletionSource<(ushort, byte[])>();
        if (SlimeTD_WsSend(socket, frame, frame.Length) == 0)
            throw new InvalidOperationException("연결이 열려 있지 않습니다");

        var (id, responseBody) = await _response.Task;
        if (id != (ushort)responseId)
            throw new InvalidOperationException($"응답 ID가 다릅니다: {id} (기대 {(ushort)responseId})");

        return parser.ParseFrom(responseBody);
    }

    [MonoPInvokeCallback(typeof(OpenCallback))]
    private static void OnOpen(int socket)
    {
        _open?.TrySetResult();
    }

    // 서버가 밀린 응답을 한 메시지에 합쳐 보낼 수 있어서, 메시지 경계가 아니라 바이트 스트림으로 프레임을 자른다
    [MonoPInvokeCallback(typeof(MessageCallback))]
    private static void OnMessage(int socket, IntPtr data, int length)
    {
        if (_count + length > _buffer.Length)
            Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, _count + length));

        Marshal.Copy(data, _buffer, _count, length);
        _count += length;

        while (_count >= 4)
        {
            int frameLength = (int)BinaryPrimitives.ReadUInt32BigEndian(_buffer.AsSpan(0, 4));
            if (_count < 4 + frameLength)
                break;

            ushort id = BinaryPrimitives.ReadUInt16BigEndian(_buffer.AsSpan(4, 2));
            byte[] body = _buffer.AsSpan(14, frameLength - 10).ToArray();

            Buffer.BlockCopy(_buffer, 4 + frameLength, _buffer, 0, _count - 4 - frameLength);
            _count -= 4 + frameLength;

            _response?.TrySetResult((id, body));
        }
    }

    [MonoPInvokeCallback(typeof(CloseCallback))]
    private static void OnClose(int socket, int code)
    {
        var error = new InvalidOperationException($"연결이 닫혔습니다 (코드 {code})");
        _open?.TrySetException(error);
        _response?.TrySetException(error);
        _count = 0;
    }
}
#endif
