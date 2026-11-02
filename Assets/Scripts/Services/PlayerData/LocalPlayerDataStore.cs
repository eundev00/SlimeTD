using System;
using System.IO;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class LocalPlayerDataStore : IPlayerDataStore
{
    private const string FileName = "player.json";

    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

#if UNITY_WEBGL && !UNITY_EDITOR
    // WebGL은 스레드가 없어 비동기 파일 API를 쓰지 않는다. 쓴 뒤에는 IndexedDB로 직접 내보내야 새로고침 후에도 남는다.
    [DllImport("__Internal")]
    private static extern void SlimeTD_SyncPersistentData();
#endif

    public async UniTask<PlayerData> LoadAsync()
    {
        string path = FilePath;
        if (!File.Exists(path))
            return null;

        try
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string json = File.ReadAllText(path);
            await UniTask.CompletedTask;
#else
            string json = await File.ReadAllTextAsync(path);
#endif
            return JsonUtility.FromJson<PlayerData>(json);
        }
        catch (Exception e)
        {
            Debug.Log($"[LocalPlayerDataStore] 저장 데이터를 읽지 못했습니다: {e.Message}");
            return null;
        }
    }

    public async UniTask SaveAsync(PlayerData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data);
#if UNITY_WEBGL && !UNITY_EDITOR
            File.WriteAllText(FilePath, json);
            SlimeTD_SyncPersistentData();
            await UniTask.CompletedTask;
#else
            await File.WriteAllTextAsync(FilePath, json);
#endif
        }
        catch (Exception e)
        {
            Debug.Log($"[LocalPlayerDataStore] 저장에 실패했습니다: {e.Message}");
        }
    }
}
