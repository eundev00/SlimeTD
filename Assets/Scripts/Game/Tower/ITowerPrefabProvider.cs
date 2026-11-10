using Cysharp.Threading.Tasks;
using UnityEngine;

public interface ITowerPrefabProvider
{
    UniTask LoadAsync(TowerCatalog catalog);
    bool TryGet(TowerData data, out GameObject prefab);
}
