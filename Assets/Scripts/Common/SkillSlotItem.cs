using UnityEngine;
using UnityEngine.UI;

public class SkillSlotItem : MonoBehaviour
{
    [NotNull][SerializeField] private Image _icon;
    [NotNull][SerializeField] private GameObject _lock;
    [NotNull][SerializeField] private GameObject _upgradeArrow;

    public void Show(Sprite icon, bool locked, bool hasNextUpgrade)
    {
        gameObject.SetActive(true);

        _icon.sprite = icon;
        _icon.enabled = icon != null;

        _lock.SetActive(locked);
        _upgradeArrow.SetActive(hasNextUpgrade);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
