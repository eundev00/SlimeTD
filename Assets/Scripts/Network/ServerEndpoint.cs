using System;
using UnityEngine;

public static class ServerEndpoint
{
    private const string ServerParameter = "server";

    // WebGL은 터널 주소가 바뀌어도 다시 빌드하지 않도록 페이지 URL의 ?server=wss://…/ws로 받는다
    public static bool TryResolve(ServerConfig config, out string address)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        address = GetQueryParameter(Application.absoluteURL, ServerParameter);
#else
        address = config != null ? $"{config.TcpHost}:{config.TcpPort}" : null;
#endif
        return !string.IsNullOrEmpty(address);
    }

    public static IServerTransport CreateTransport()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return new WebSocketServerTransport();
#else
        return new TcpServerTransport();
#endif
    }

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
}