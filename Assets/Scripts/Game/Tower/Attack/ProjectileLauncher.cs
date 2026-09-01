using UnityEngine;

public class ProjectileLauncher : MonoBehaviour
{
    [NotNull][SerializeField] private Transform _firePoint;
    [NotNull][SerializeField] private GameObject _heldProjectile;

    public Transform FirePoint => _firePoint != null ? _firePoint : transform;

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
