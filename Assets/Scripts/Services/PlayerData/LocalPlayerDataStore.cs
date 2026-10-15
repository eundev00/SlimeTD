using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class LocalPlayerDataStore : IPlayerDataStore
{
    private const string FileName = "player.json";

    private string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public async UniTask<PlayerData> LoadAsync()
    {
        string path = FilePath;
        if (!File.Exists(path))
            return null;

        try
        {
            string json = await File.ReadAllTextAsync(path);
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
            await File.WriteAllTextAsync(FilePath, json);
        }
        catch (Exception e)
        {
            Debug.Log($"[LocalPlayerDataStore] 저장에 실패했습니다: {e.Message}");
        }
    }
}