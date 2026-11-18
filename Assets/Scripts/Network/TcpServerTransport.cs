using System;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TcpServerTransport : IServerTransport
{
    private TcpClient _client;
    private NetworkStream _stream;
    private bool _closed;

    public event Action<byte[], int> Received;
    public event Action<string> Closed;

    public bool IsOpen => _client != null && _client.Connected && !_closed;

    // address는 host:port
    public async UniTask ConnectAsync(string address)
    {
        int colon = address.LastIndexOf(':');
        string host = address.Substring(0, colon);
        int port = int.Parse(address.Substring(colon + 1));

        _client = new TcpClient { NoDelay = true };
        await _client.ConnectAsync(host, port);
        await UniTask.SwitchToMainThread();
        _stream = _client.GetStream();
        _closed = false;
        ReceiveLoopAsync().Forget();
    }

    public void Send(byte[] frame)
    {
        if (!IsOpen)
            throw new InvalidOperationException("서버에 연결되어 있지 않습니다");

        _stream.Write(frame, 0, frame.Length);
    }

    private async UniTaskVoid ReceiveLoopAsync()
    {
        var buffer = new byte[8192];
        string reason = "서버가 연결을 끊었습니다";

        try
        {
            while (!_closed)
            {
                int read = await _stream.ReadAsync(buffer, 0, buffer.Length);
                await UniTask.SwitchToMainThread();
                if (read == 0)
                    break;

                Received?.Invoke(buffer, read);
            }
        }
        catch (Exception e)
        {
            await UniTask.SwitchToMainThread();
            reason = e.Message;
        }

        Close(reason);
    }

    private void Close(string reason)
    {
        if (_closed)
            return;

        _closed = true;
        _stream?.Dispose();
        _client?.Dispose();
        Closed?.Invoke(reason);
    }

    public void Dispose()
    {
        Close("연결을 닫았습니다");
    }
}