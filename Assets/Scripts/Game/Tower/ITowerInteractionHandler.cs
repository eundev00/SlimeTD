using UniRx;
using UnityEngine;

public interface ITowerInteractionHandler
{
    IReadOnlyReactiveProperty<bool> IsSelected { get; }
    IReadOnlyReactiveProperty<bool> IsDragging { get; }

    void Select();
    void Deselect();

    void BeginDrag();
    void UpdateDragPosition(Vector3 worldPosition, bool isValid);
    void EndDrag(Vector3 snappedWorldPosition);
    void CancelDrag();
}
