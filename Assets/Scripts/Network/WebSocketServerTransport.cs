#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using Cysharp.Threading.Tasks;

// WebGL은 소켓을 쓸 수 없어 브라우저 WebSocket(SlimeTDWebSocket.jslib)을 쓴다. 바이너리 메시지만 주고받는다
public class WebSocketServerTransport : IServerTransport
{
    private delegate void OpenCallback(int socket);
    private delegate void MessageCallback(int socket, IntPtr data, int length);
    private delegate void CloseCallback(int socket, int code);

    [DllImport("__Internal")]
    private static extern int SlimeTD_WsConnect(string url, OpenCallback onOpen, MessageCallback onMessage, CloseCallback onClose);

    [DllImport("__Internal")]
    private static extern int SlimeTD_WsSend(int socket, byte[] data, int length);

    [DllImport("__Internal")]
    private static extern void SlimeTD_WsClose(int socket);

    private static readonly Dictionary<int, WebSocketServerTransport> Sockets = new Dictionary<int, WebSocketServerTransport>();

    private int _socket;
    private bool _open;
    private UniTaskCompletionSource _connecting;
    private byte[] _receiveBuffer = new byte[4096];

    public event Action<byte[], int> Received;
    public event Action<string> Closed;

    public bool IsOpen => _open;

    public async UniTask ConnectAsync(string address)
    {
        _connecting = new UniTaskCompletionSource();
        _socket = SlimeTD_WsConnect(address, OnOpen, OnMessage, OnClose);
        Sockets[_socket] = this;
        await _connecting.Task;
    }

    public void Send(byte[] frame)
    {
        if (!_open || SlimeTD_WsSend(_socket, frame, frame.Length) == 0)
            throw new InvalidOperationException("서버에 연결되어 있지 않습니다");
    }

    public void Dispose()
    {
        if (_socket != 0)
            SlimeTD_WsClose(_socket);
    }

    [MonoPInvokeCallback(typeof(OpenCallback))]
    private static void OnOpen(int socket)
    {
        if (!Sockets.TryGetValue(socket, out var transport))
            return;

        transport._open = true;
        transport._connecting?.TrySetResult();
    }

    [MonoPInvokeCallback(typeof(MessageCallback))]
    private static void OnMessage(int socket, IntPtr data, int length)
    {
        if (!Sockets.TryGetValue(socket, out var transport))
            return;

        if (transport._receiveBuffer.Length < length)
            transport._receiveBuffer = new byte[length];

        Marshal.Copy(data, transport._receiveBuffer, 0, length);
        transport.Received?.Invoke(transport._receiveBuffer, length);
    }

    [MonoPInvokeCallback(typeof(CloseCallback))]
    private static void OnClose(int socket, int code)
    {
        if (!Sockets.TryGetValue(socket, out var transport))
            return;

        Sockets.Remove(socket);
        string reason = $"연결이 닫혔습니다 (코드 {code})";
        transport._open = false;
        transport._connecting?.TrySetException(new InvalidOperationException(reason));
        transport.Closed?.Invoke(reason);
    }
}
#endif