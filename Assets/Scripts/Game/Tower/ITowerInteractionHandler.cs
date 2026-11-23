using UniRx;

public interface ITowerInteractionHandler
{
    IReadOnlyReactiveProperty<bool> IsSelected { get; }

    void Select();
    void Deselect();
}
