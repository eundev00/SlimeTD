using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class SkillTooltip : MonoBehaviour
{
    [NotNull][SerializeField] private TMP_Text _unlockText;
    [NotNull][SerializeField] private TMP_Text _nameText;
    [NotNull][SerializeField] private TMP_Text _stageText;
    [NotNull][SerializeField] private TMP_Text _descriptionText;
    [NotNull][SerializeField] private GameObject _divider;
    [NotNull][SerializeField] private TMP_Text _nextHeaderText;
    [NotNull][SerializeField] private TMP_Text _nextNameText;
    [NotNull][SerializeField] private TMP_Text _nextDescriptionText;

    // 이 영역(자기 슬롯) 안을 누른 경우는 슬롯 버튼이 열고 닫기를 처리하므로 바깥 터치로 보지 않는다.
    [SerializeField] private RectTransform _ignoreArea;

    private InputAction _pressAction;

    public bool IsOpen => gameObject.activeSelf;

    public void SetContent(in SkillTooltipContent content)
    {
        SetLine(_unlockText, content.UnlockText);
        _nameText.text = content.Name;
        SetLine(_stageText, content.Stage);
        _descriptionText.text = content.Description;

        _divider.SetActive(content.HasNext);
        SetLine(_nextHeaderText, content.HasNext ? content.NextHeader : null);
        SetLine(_nextNameText, content.HasNext ? content.NextName : null);
        SetLine(_nextDescriptionText, content.HasNext ? content.NextDescription : null);
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        _pressAction = new InputAction(type: InputActionType.Button, binding: "<Pointer>/press");
        _pressAction.performed += OnPressed;
        _pressAction.Enable();
    }

    private void OnDisable()
    {
        if (_pressAction != null)
        {
            _pressAction.performed -= OnPressed;
            _pressAction.Dispose();
            _pressAction = null;
        }
    }

    // 누른 것을 막지 않고 툴팁만 닫는다. 닫기 버튼 같은 다른 UI는 원래대로 동작한다.
    private void OnPressed(InputAction.CallbackContext context)
    {
        var pointer = Pointer.current;
        if (pointer == null)
            return;

        Vector2 position = pointer.position.ReadValue();
        var canvas = GetComponentInParent<Canvas>();
        var cam = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.rootCanvas.worldCamera
            : null;

        if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, position, cam))
            return;

        if (_ignoreArea != null && RectTransformUtility.RectangleContainsScreenPoint(_ignoreArea, position, cam))
            return;

        Close();
    }

    private static void SetLine(TMP_Text text, string value)
    {
        bool visible = !string.IsNullOrEmpty(value);
        text.gameObject.SetActive(visible);
        if (visible)
            text.text = value;
    }
}
