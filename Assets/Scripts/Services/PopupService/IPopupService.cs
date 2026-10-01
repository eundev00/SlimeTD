using Cysharp.Threading.Tasks;

namespace Services.PopupService
{
    public interface IPopupService
    {
        bool HasOpenPopup { get; }

        UniTask<T> OpenAsync<T>(string key) where T : PopupBase;
        UniTask CloseAsync(PopupBase popup);
    }
}
