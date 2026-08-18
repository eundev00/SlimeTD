using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;

namespace Services.PoolService
{
    public class GameObjectPoolService : IGameObjectPoolService, IDisposable
    {
        private readonly IObjectResolver _resolver;
        private readonly Dictionary<GameObject, ObjectPool<GameObject>> _pools = new();
        private readonly Dictionary<GameObject, GameObject> _instanceToPrefab = new();
        private readonly Transform _poolRoot;

        [Inject]
        public GameObjectPoolService(IObjectResolver resolver)
        {
            _resolver = resolver;
            _poolRoot = new GameObject("[PoolRoot]").transform;
        }

        public void CreatePool(GameObject prefab, int defaultCapacity = 10, int maxSize = 100)
        {
            if (prefab == null)
            {
                Debug.LogError("[GameObjectPoolService] 프리팹이 null입니다.");
                return;
            }

            if (_pools.ContainsKey(prefab))
                return;

            var pool = new ObjectPool<GameObject>(
                createFunc: () => CreateInstance(prefab),
                actionOnGet: OnGetInstance,
                actionOnRelease: OnReleaseInstance,
                actionOnDestroy: OnDestroyInstance,
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );

            _pools[prefab] = pool;
        }

        public GameObject Get(GameObject prefab)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                Debug.LogError($"[GameObjectPoolService] 등록되지 않은 프리팹: {prefab.name}. CreatePool을 먼저 호출하세요.");
                return null;
            }

            return pool.Get();
        }

        public void Release(GameObject instance)
        {
            if (instance == null)
                return;

            if (!_instanceToPrefab.TryGetValue(instance, out var prefab))
            {
                Debug.LogError($"[GameObjectPoolService] 풀에서 관리하지 않는 인스턴스: {instance.name}", instance);
                return;
            }

            if (!_pools.TryGetValue(prefab, out var pool))
                return;

            pool.Release(instance);
        }

        public void Dispose()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }

            _pools.Clear();
            _instanceToPrefab.Clear();

            if (_poolRoot != null)
            {
                UnityEngine.Object.Destroy(_poolRoot.gameObject);
            }
        }

        private GameObject CreateInstance(GameObject prefab)
        {
            var instance = UnityEngine.Object.Instantiate(prefab, _poolRoot);
            instance.SetActive(false);

            // GameObject의 모든 컴포넌트에 VContainer 의존성 주입
            foreach (var component in instance.GetComponentsInChildren<MonoBehaviour>(true))
            {
                _resolver.Inject(component);
            }

            _instanceToPrefab[instance] = prefab;
            return instance;
        }

        private void OnGetInstance(GameObject instance)
        {
            instance.SetActive(true);

            var poolables = instance.GetComponents<IPoolable>();
            foreach (var poolable in poolables)
            {
                poolable.OnGetFromPool();
            }
        }

        private void OnReleaseInstance(GameObject instance)
        {
            var poolables = instance.GetComponents<IPoolable>();
            foreach (var poolable in poolables)
            {
                poolable.OnReturnToPool();
            }

            instance.SetActive(false);
            instance.transform.SetParent(_poolRoot);
        }

        private void OnDestroyInstance(GameObject instance)
        {
            _instanceToPrefab.Remove(instance);
            UnityEngine.Object.Destroy(instance);
        }
    }
}
