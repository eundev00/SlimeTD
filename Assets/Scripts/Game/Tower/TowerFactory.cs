using UnityEngine;
using VContainer;

public static class TowerFactory
{
    public static GameObject Create(IObjectResolver resolver, GameObject prefab, TowerData towerData, Transform inactiveHolder, Transform parent, Vector3 position)
    {
        // 비활성 부모 밑에 생성해야 Awake가 안 돈다. 프리팹 자체를 SetActive(false)하면 에셋이 오염된다.
        var instance = Object.Instantiate(prefab, inactiveHolder);

        foreach (var component in instance.GetComponentsInChildren<MonoBehaviour>(true))
        {
            resolver.Inject(component);
        }

        instance.transform.SetParent(parent, false);
        instance.transform.position = position;
        instance.transform.rotation = FaceCamera(position);

        var tower = instance.GetComponent<BaseTower>();
        if (tower != null)
        {
            tower.Initialize(towerData);
        }

        return instance;
    }

    private static Quaternion FaceCamera(Vector3 position)
    {
        var mainCamera = Camera.main;
        if (mainCamera == null)
            return Quaternion.identity;

        var directionToCamera = mainCamera.transform.position - position;
        directionToCamera.y = 0;
        return directionToCamera.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(directionToCamera)
            : Quaternion.identity;
    }
}
