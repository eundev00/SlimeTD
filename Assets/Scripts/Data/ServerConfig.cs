using UnityEngine;

// TODO: 서버 접속 테스트용 주소 설정. 정식 네트워크 계층이 생기면 다시 정한다
[CreateAssetMenu(fileName = "ServerConfig", menuName = "SlimeTD/Server Config")]
public class ServerConfig : ScriptableObject
{
    [SerializeField] private string _tcpHost = "127.0.0.1";
    [SerializeField] private int _tcpPort = 30000;
    [SerializeField] private string _webSocketUrl = "ws://127.0.0.1:30001/ws";

    public string TcpHost => _tcpHost;
    public int TcpPort => _tcpPort;
    public string WebSocketUrl => _webSocketUrl;
}