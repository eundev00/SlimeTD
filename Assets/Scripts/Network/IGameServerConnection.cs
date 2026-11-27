using Cysharp.Threading.Tasks;
using Google.Protobuf;
using SlimeTD.Protocol;
using UniRx;

public interface IGameServerConnection
{
    IReadOnlyReactiveProperty<bool> IsConnected { get; }

    // 서버 시계 기준 현재 시각 (Unix ms). 기기 시계가 틀려도 서버가 보낸 시각과 맞는다
    long ServerNowMs { get; }

    UniTask<bool> ConnectAsync();
    UniTask<TResponse> RequestAsync<TResponse>(MsgId requestId, IMessage request, MsgId responseId, MessageParser<TResponse> parser)
        where TResponse : IMessage<TResponse>;

    // 서버가 요청 없이 보내는 알림 (메시지 이름이 Ntf로 끝난다). 메인 스레드에서 온다
    System.IObservable<TMessage> ObserveNotification<TMessage>(MsgId id, MessageParser<TMessage> parser)
        where TMessage : IMessage<TMessage>;
}