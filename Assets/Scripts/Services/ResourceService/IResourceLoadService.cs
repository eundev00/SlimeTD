using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;

public interface IResourceLoadService
{
    UniTask<T> LoadAsync<T>(string key) where T : Object;
    T Get<T>(string key) where T : Object;
    bool IsLoaded(string key);
    void Release(string key);
    void ReleaseAll();
}
