using Cysharp.Threading.Tasks;
using UnityEngine;

public interface ISlimePrefabProvider
{
    UniTask LoadAsync(StageData stage);
    bool TryGet(SlimeDataBase data, out GameObject prefab);
}
