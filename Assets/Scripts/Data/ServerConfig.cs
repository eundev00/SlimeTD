using UnityEngine;

// TODO: 에디터 서버 접속 테스트(TCP)용 주소. WebGL은 페이지 URL의 server 파라미터를 쓴다
[CreateAssetMenu(fileName = "ServerConfig", menuName = "SlimeTD/Server Config")]
public class ServerConfig : ScriptableObject
{
    [SerializeField] private string _tcpHost = "127.0.0.1";
    [SerializeField] private int _tcpPort = 30000;

    public string TcpHost => _tcpHost;
    public int TcpPort => _tcpPort;
}