using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class SkillTooltip : MonoBehaviour
{
    [NotNull][SerializeField] private TMP_Text _unlockText;
    [NotNull][SerializeField] private TMP_Text _nameText;
    [NotNull][SerializeField] private TMP_Text _descriptionText;

    // 남은 단계 수만큼 복제해서 쓴다 (3단계 계열은 2칸).
    [NotNull][SerializeField] private SkillTooltipStageView _stageTemplate;

    // 이 영역(자기 슬롯) 안을 누른 경우는 슬롯 버튼이 열고 닫기를 처리하므로 바깥 터치로 보지 않는다.
    [SerializeField] private RectTransform _ignoreArea;

    private readonly List<SkillTooltipStageView> _stageViews = new List<SkillTooltipStageView>();
    private InputAction _pressAction;

    public bool IsOpen => gameObject.activeSelf;

    public void SetContent(in SkillTooltipContent content)
    {
        SetLine(_unlockText, content.UnlockText);
        _nameText.text = content.Name;
        _descriptionText.text = content.Description;

        if (_stageViews.Count == 0)
            _stageViews.Add(_stageTemplate);

        var upcoming = content.Upcoming;
        while (_stageViews.Count < upcoming.Count)
            _stageViews.Add(Instantiate(_stageTemplate, _stageTemplate.transform.parent));

        for (int i = 0; i < _stageViews.Count; i++)
        {
            if (i < upcoming.Count)
                _stageViews[i].Show(upcoming[i]);
            else
                _stageViews[i].Hide();
        }
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
