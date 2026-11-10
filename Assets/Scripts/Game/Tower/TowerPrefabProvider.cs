using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 타워 수치는 서버 데이터로 바뀔 예정이라 프리팹은 데이터가 아니라 CodeName(프리팹 이름)으로 찾는다
public class TowerPrefabProvider : ITowerPrefabProvider
{
    private readonly IResourceLoadService _resourceLoadService;
    private readonly Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();

    public TowerPrefabProvider(IResourceLoadService resourceLoadService)
    {
        _resourceLoadService = resourceLoadService;
    }

    public async UniTask LoadAsync(TowerCatalog catalog)
    {
        if (catalog == null || catalog.Towers == null)
            return;

        foreach (var tower in catalog.Towers)
        {
            if (tower == null || !tower.Enabled || _prefabs.ContainsKey(tower.CodeName))
                continue;

            var prefab = await _resourceLoadService.LoadAsync<GameObject>(tower.CodeName);
            if (prefab == null)
            {
                Debug.Log($"[TowerPrefabProvider] 타워 프리팹을 찾지 못했습니다: {tower.CodeName}");
                continue;
            }

            _prefabs[tower.CodeName] = prefab;
        }
    }

    public bool TryGet(TowerData data, out GameObject prefab)
    {
        prefab = null;
        return data != null && _prefabs.TryGetValue(data.CodeName, out prefab) && prefab != null;
    }
}
