using Cysharp.Threading.Tasks;
using Google.Protobuf;
using SlimeTD.Protocol;
using UniRx;

public interface IGameServerConnection
{
    IReadOnlyReactiveProperty<bool> IsConnected { get; }

    UniTask<bool> ConnectAsync();
    UniTask<TResponse> RequestAsync<TResponse>(MsgId requestId, IMessage request, MsgId responseId, MessageParser<TResponse> parser)
        where TResponse : IMessage<TResponse>;
}