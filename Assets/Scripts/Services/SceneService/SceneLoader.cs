using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : ISceneLoader
{
    private bool _isTransitioning;

    public async UniTask LoadAdditiveAsync(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive).ToUniTask();

        var loaded = SceneManager.GetSceneByName(sceneName);
        if (!loaded.IsValid())
        {
            Debug.LogError($"[SceneLoader] 씬 로드 실패: {sceneName} (Build Settings 등록 확인)");
            return;
        }

        SceneManager.SetActiveScene(loaded);
    }

    public async UniTask UnloadAsync(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        var scene = SceneManager.GetSceneByName(sceneName);

        // 미로드 씬에 UnloadSceneAsync를 호출하면 null이 반환된다
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        await SceneManager.UnloadSceneAsync(scene).ToUniTask();
    }

    public async UniTask TransitionAsync(string fromSceneName, string toSceneName)
    {
        if (_isTransitioning)
            return;

        _isTransitioning = true;

        try
        {
            // TODO: 페이드 아웃

            // 활성 씬을 먼저 언로드하면 Unity가 임의의 씬을 활성 씬으로 잡는다
            await LoadAdditiveAsync(toSceneName);
            await UnloadAsync(fromSceneName);

            // TODO: 페이드 인
        }
        finally
        {
            _isTransitioning = false;
        }
    }
}
