using UnityEngine;

public class ProjectileLauncher : MonoBehaviour
{
    [NotNull][SerializeField] private Transform _firePoint;
    [SerializeField] private GameObject _heldProjectile;
    [SerializeField] private float _multiShotSpacing = 0.4f;

    public Transform FirePoint => _firePoint != null ? _firePoint : transform;
    public float MultiShotSpacing => _multiShotSpacing;

    private void Awake()
    {
        SetHeldProjectileActive(false);
    }

    public void SetHeldProjectileActive(bool active)
    {
        if (_heldProjectile != null)
            _heldProjectile.SetActive(active);
    }
}
