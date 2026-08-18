using UnityEngine;

namespace Services.PoolService
{
    public interface IGameObjectPoolService
    {
        void CreatePool(GameObject prefab, int defaultCapacity = 10, int maxSize = 100);
        GameObject Get(GameObject prefab);
        void Release(GameObject instance);
    }
}
