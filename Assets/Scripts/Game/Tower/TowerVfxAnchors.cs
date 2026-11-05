using UnityEngine;

public class TowerVfxAnchors : MonoBehaviour
{
    [SerializeField] private Transform _center;
    [SerializeField] private Transform _head;
    [SerializeField] private Transform _front;

    // 지정하지 않은 앵커는 타워 루트(발밑)로 대신한다.
    public Transform Get(VfxAnchor anchor)
    {
        Transform result = anchor switch
        {
            VfxAnchor.Center => _center,
            VfxAnchor.Head => _head,
            VfxAnchor.Front => _front,
            _ => null,
        };

        return result != null ? result : transform;
    }
}
