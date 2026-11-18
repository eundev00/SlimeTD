using System;
using Cysharp.Threading.Tasks;

public interface IServerTransport : IDisposable
{
    // 받은 바이트와 끊김은 메인 스레드에서 알린다
    event Action<byte[], int> Received;
    event Action<string> Closed;

    bool IsOpen { get; }

    UniTask ConnectAsync(string address);
    void Send(byte[] frame);
}