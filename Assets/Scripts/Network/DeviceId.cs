using System;
using UnityEngine;

public static class DeviceId
{
    private const string Key = "SlimeTD.DeviceId";

    // WebGL은 deviceUniqueIdentifier가 비거나 매번 달라질 수 있어 처음 만든 GUID를 저장해 쓴다
    public static string Get()
    {
#if !(UNITY_WEBGL && !UNITY_EDITOR)
        string id = SystemInfo.deviceUniqueIdentifier;
        if (!string.IsNullOrEmpty(id) && id != SystemInfo.unsupportedIdentifier)
            return id;
#endif
        string saved = PlayerPrefs.GetString(Key, string.Empty);
        if (!string.IsNullOrEmpty(saved))
            return saved;

        saved = Guid.NewGuid().ToString("N");
        PlayerPrefs.SetString(Key, saved);
        PlayerPrefs.Save();
        return saved;
    }
}