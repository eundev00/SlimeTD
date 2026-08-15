using Cysharp.Threading.Tasks;

public interface ISceneLoader
{
    // 로드 후 Active Scene으로 지정한다.
    UniTask LoadAdditiveAsync(string sceneName);

    UniTask UnloadAsync(string sceneName);

    UniTask TransitionAsync(string fromSceneName, string toSceneName);
}
