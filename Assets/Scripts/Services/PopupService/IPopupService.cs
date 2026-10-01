using Cysharp.Threading.Tasks;

public interface IPopupService
{
    bool HasOpenPopup { get; }

    UniTask<PopupBase> OpenAsync(string key);
    UniTask<T> OpenAsync<T>(string key) where T : PopupBase;
    UniTask CloseAsync(PopupBase popup);
}
