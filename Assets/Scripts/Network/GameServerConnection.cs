using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using SlimeTD.Protocol;
using UniRx;
using UnityEngine;

public class GameServerConnection : IGameServerConnection, IDisposable
{
    private readonly IResourceLoadService _resourceLoadService;
    private readonly ReactiveProperty<bool> _isConnected = new ReactiveProperty<bool>();
    private readonly ServerFrameCodec.Reader _reader = new ServerFrameCodec.Reader();
    private readonly Queue<(MsgId Expected, UniTaskCompletionSource<byte[]> Source)> _pending =
        new Queue<(MsgId, UniTaskCompletionSource<byte[]>)>();
    private readonly Subject<(MsgId Id, byte[] Body)> _notifications = new Subject<(MsgId, byte[])>();
    private static readonly HashSet<MsgId> NotificationIds = CollectNotificationIds();

    private IServerTransport _transport;
    private long _serverOffsetMs;

    public IReadOnlyReactiveProperty<bool> IsConnected => _isConnected;

    // 기기 시계와 서버 시계의 차이를 응답마다 맞춘다 (ts가 초 단위라 1초 이내 오차)
    public long ServerNowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _serverOffsetMs;

    public GameServerConnection(IResourceLoadService resourceLoadService)
    {
        _resourceLoadService = resourceLoadService;
    }

    public async UniTask<bool> ConnectAsync()
    {
        if (_isConnected.Value)
            return true;

        var config = await _resourceLoadService.LoadAsync<ServerConfig>(DataKeys.ServerConfig);
        if (!ServerEndpoint.TryResolve(config, out string address))
        {
            Debug.Log("[GameServerConnection] 서버 주소가 없습니다. WebGL은 페이지 주소에 ?server=wss://…/ws를 붙여야 합니다.");
            return false;
        }

        CloseTransport();
        _reader.Clear();
        _transport = ServerEndpoint.CreateTransport();
        _transport.Received += OnReceived;
        _transport.Closed += OnClosed;

        try
        {
            Debug.Log($"[GameServerConnection] 접속: {address}");
            await _transport.ConnectAsync(address);
        }
        catch (Exception e)
        {
            Debug.Log($"[GameServerConnection] 접속 실패: {address} ({e.Message})");
            CloseTransport();
            return false;
        }

        _isConnected.Value = true;
        return true;
    }

    // 서버는 한 연결의 요청을 순서대로 처리하므로, 응답은 보낸 순서대로 대기 목록 맨 앞과 짝짓는다
    public async UniTask<TResponse> RequestAsync<TResponse>(MsgId requestId, IMessage request, MsgId responseId, MessageParser<TResponse> parser)
        where TResponse : IMessage<TResponse>
    {
        if (!_isConnected.Value || _transport == null)
            throw new InvalidOperationException("서버에 연결되어 있지 않습니다");

        var source = new UniTaskCompletionSource<byte[]>();
        _pending.Enqueue((responseId, source));
        _transport.Send(ServerFrameCodec.Encode(requestId, request));

        byte[] body = await source.Task;
        return parser.ParseFrom(body);
    }

    private void OnReceived(byte[] data, int length)
    {
        _reader.Append(data, length);

        while (_reader.TryRead(out var id, out var body, out uint serverTime))
        {
            if (serverTime > 0)
                _serverOffsetMs = serverTime * 1000L - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // 알림은 요청 순서와 상관없이 끼어 오므로 응답 대기열과 짝짓지 않는다
            if (NotificationIds.Contains(id))
            {
                _notifications.OnNext((id, body));
                continue;
            }

            if (_pending.Count == 0)
            {
                Debug.Log($"[GameServerConnection] 기다리는 요청이 없는 응답을 받았습니다: {id}");
                continue;
            }

            var (expected, source) = _pending.Dequeue();
            if (id != expected)
                source.TrySetException(new InvalidOperationException($"응답 ID가 다릅니다: {id} (기대 {expected})"));
            else
                source.TrySetResult(body);
        }
    }

    public IObservable<TMessage> ObserveNotification<TMessage>(MsgId id, MessageParser<TMessage> parser)
        where TMessage : IMessage<TMessage>
    {
        return _notifications.Where(n => n.Id == id).Select(n => parser.ParseFrom(n.Body));
    }

    private static HashSet<MsgId> CollectNotificationIds()
    {
        var ids = new HashSet<MsgId>();
        foreach (MsgId id in Enum.GetValues(typeof(MsgId)))
        {
            if (id.ToString().EndsWith("Ntf", StringComparison.Ordinal))
                ids.Add(id);
        }
        return ids;
    }

    private void OnClosed(string reason)
    {
        Debug.Log($"[GameServerConnection] 연결이 끊겼습니다: {reason}");
        _isConnected.Value = false;

        var error = new InvalidOperationException($"서버 연결이 끊겼습니다: {reason}");
        while (_pending.Count > 0)
            _pending.Dequeue().Source.TrySetException(error);
    }

    private void CloseTransport()
    {
        if (_transport == null)
            return;

        _transport.Received -= OnReceived;
        _transport.Closed -= OnClosed;
        _transport.Dispose();
        _transport = null;
    }

    public void Dispose()
    {
        CloseTransport();
        _isConnected.Dispose();
        _notifications.Dispose();
    }
}