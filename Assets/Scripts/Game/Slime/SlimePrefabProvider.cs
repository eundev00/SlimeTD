using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 슬라임 수치는 서버 데이터로 바뀔 예정이라 프리팹은 데이터가 아니라 CodeName(프리팹 이름)으로 찾는다
public class SlimePrefabProvider : ISlimePrefabProvider
{
    private readonly IResourceLoadService _resourceLoadService;
    private readonly Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();

    public SlimePrefabProvider(IResourceLoadService resourceLoadService)
    {
        _resourceLoadService = resourceLoadService;
    }

    public async UniTask LoadAsync(StageData stage)
    {
        if (stage == null)
            return;

        for (int wave = 1; wave <= stage.FinalWave; wave++)
        {
            foreach (var group in stage.GetWave(wave).Groups)
            {
                var slime = group?.Slime;
                if (slime == null || string.IsNullOrEmpty(slime.CodeName) || _prefabs.ContainsKey(slime.CodeName))
                    continue;

                var prefab = await _resourceLoadService.LoadAsync<GameObject>(slime.CodeName);
                if (prefab == null)
                {
                    Debug.Log($"[SlimePrefabProvider] 슬라임 프리팹을 찾지 못했습니다: {slime.CodeName}");
                    continue;
                }

                _prefabs[slime.CodeName] = prefab;
            }
        }
    }

    public bool TryGet(SlimeDataBase data, out GameObject prefab)
    {
        prefab = null;
        return data != null && !string.IsNullOrEmpty(data.CodeName) && _prefabs.TryGetValue(data.CodeName, out prefab) && prefab != null;
    }
}
