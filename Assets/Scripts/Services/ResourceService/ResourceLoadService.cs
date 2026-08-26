using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

public class ResourceLoadService : IResourceLoadService, IDisposable
{
    private readonly Dictionary<string, AsyncOperationHandle> _handles = new();
    private readonly Dictionary<string, UniTask<Object>> _loading = new();

    public async UniTask<T> LoadAsync<T>(string key) where T : Object
    {
        if (_handles.TryGetValue(key, out var loaded))
            return loaded.Result as T;

        // 같은 키를 동시에 요청하면 핸들이 중복 생성되어 한쪽이 해제되지 않는다
        if (_loading.TryGetValue(key, out var inFlight))
            return await inFlight as T;

        var path = ResourceKeys.GetPath(key);
        if (string.IsNullOrEmpty(path))
        {
            Debug.Log($"[ResourceLoadService] 키 테이블에 없는 키: {key}");
            return null;
        }

        var task = LoadInternalAsync<T>(key, path);
        _loading[key] = task;

        try
        {
            return await task as T;
        }
        finally
        {
            _loading.Remove(key);
        }
    }

    private async UniTask<Object> LoadInternalAsync<T>(string key, string path) where T : Object
    {
        var handle = Addressables.LoadAssetAsync<T>(path);
        var asset = await handle;

        if (handle.Status != AsyncOperationStatus.Succeeded || asset == null)
        {
            Debug.Log($"[ResourceLoadService] 로드 실패: {key} ({path})");
            Addressables.Release(handle);
            return null;
        }

        _handles[key] = handle;
        return asset;
    }

    public T Get<T>(string key) where T : Object
    {
        return _handles.TryGetValue(key, out var handle) ? handle.Result as T : null;
    }

    public bool IsLoaded(string key)
    {
        return _handles.ContainsKey(key);
    }

    public void Release(string key)
    {
        if (!_handles.TryGetValue(key, out var handle))
            return;

        Addressables.Release(handle);
        _handles.Remove(key);
    }

    public void ReleaseAll()
    {
        foreach (var handle in _handles.Values)
        {
            Addressables.Release(handle);
        }

        _handles.Clear();
    }

    public void Dispose()
    {
        ReleaseAll();
    }
}
