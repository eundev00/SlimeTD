using System;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotItem : MonoBehaviour
{
    [NotNull][SerializeField] private Image _icon;
    [NotNull][SerializeField] private GameObject _lock;
    [NotNull][SerializeField] private GameObject _upgradeArrow;
    [NotNull][SerializeField] private Button _button;
    [NotNull][SerializeField] private SkillTooltip _tooltip;

    public event Action<SkillSlotItem> Clicked;

    private void Start()
    {
        if (_button == null)
        {
            Debug.Log("[SkillSlotItem] 버튼이 연결되지 않았다", this);
            return;
        }

        _button.onClick.AddListener(OnButtonClicked);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnButtonClicked);
    }

    public void Show(Sprite icon, bool locked, bool hasNextUpgrade, in SkillTooltipContent tooltip)
    {
        gameObject.SetActive(true);

        _icon.sprite = icon;
        _icon.enabled = icon != null;

        _lock.SetActive(locked);
        _upgradeArrow.SetActive(hasNextUpgrade);
        _tooltip.SetContent(tooltip);
    }

    public void Hide()
    {
        CloseTooltip();
        gameObject.SetActive(false);
    }

    public void ToggleTooltip()
    {
        if (_tooltip.IsOpen)
            _tooltip.Close();
        else
            _tooltip.Open();
    }

    public void CloseTooltip()
    {
        _tooltip.Close();
    }

    private void OnButtonClicked()
    {
        Clicked?.Invoke(this);
    }
}
