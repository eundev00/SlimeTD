using System;
using UnityEngine;
using UnityEngine.UI;

public class TowerActionView : MonoBehaviour
{
    [NotNull][SerializeField] private RectTransform _root;
    [NotNull][SerializeField] private Button _mergeButton;

    public event Action MergeButtonClicked;

    private void Awake()
    {
        Hide();
    }

    private void Start()
    {
        if (_root == null)
        {
            Debug.Log("[TowerActionView] _root가 연결되지 않았습니다.", this);
            return;
        }

        if (_mergeButton == null)
        {
            Debug.Log("[TowerActionView] _mergeButton이 연결되지 않았습니다.", this);
            return;
        }

        _mergeButton.onClick.AddListener(OnMergeButtonClicked);
    }

    private void OnDestroy()
    {
        if (_mergeButton != null)
        {
            _mergeButton.onClick.RemoveListener(OnMergeButtonClicked);
        }
    }

    private void OnMergeButtonClicked()
    {
        MergeButtonClicked?.Invoke();
    }

    public void Show()
    {
        if (_root != null)
            _root.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (_root != null)
            _root.gameObject.SetActive(false);
    }

    public void SetMergeButtonInteractable(bool interactable)
    {
        if (_mergeButton != null)
            _mergeButton.interactable = interactable;
    }

    public void SetScreenPosition(Vector2 screenPosition)
    {
        if (_root != null)
            _root.position = screenPosition;
    }
}
