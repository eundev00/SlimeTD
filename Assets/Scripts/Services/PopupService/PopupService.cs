using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Services.UpdateService;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

public class PopupService : IPopupService, IStartable, IUpdatable, IDisposable
{
    private readonly IObjectResolver _resolver;
    private readonly IResourceLoadService _resourceLoadService;
    private readonly IUpdateSubscriptionService _updateSubscriptionService;
    private readonly RectTransform _container;

    private readonly Dictionary<string, PopupBase> _instances = new();
    private readonly List<PopupBase> _openPopups = new();
    private readonly Dictionary<PopupBase, IScopedObjectResolver> _scopes = new();

    private bool _disposed;

    public bool HasOpenPopup => _openPopups.Count > 0;

    public PopupService(
        IObjectResolver resolver,
        IResourceLoadService resourceLoadService,
        IUpdateSubscriptionService updateSubscriptionService,
        RectTransform container)
    {
        _resolver = resolver;
        _resourceLoadService = resourceLoadService;
        _updateSubscriptionService = updateSubscriptionService;
        _container = container;
    }

    public void Start()
    {
        _updateSubscriptionService.RegisterUpdatable(this);
    }

    public async UniTask<PopupBase> OpenAsync(string key)
    {
        var popup = await GetOrCreateAsync(key);
        if (popup == null)
            return null;

        Open(popup);
        return popup;
    }

    public async UniTask<T> OpenAsync<T>(string key) where T : PopupBase
    {
        var popup = await GetOrCreateAsync(key);
        if (popup == null)
            return null;

        if (popup is not T typed)
        {
            Debug.Log($"[PopupService] {key} 팝업이 {typeof(T).Name} 타입이 아닙니다.", popup);
            return null;
        }

        Open(popup);
        return typed;
    }

    private void Open(PopupBase popup)
    {
        if (popup.IsOpen)
            return;

        popup.transform.SetAsLastSibling();
        _openPopups.Add(popup);
        CreateScope(popup);

        // 연출 완료를 기다리지 않고 반환해야 호출자가 같은 프레임에 내용을 채워 빈 팝업이 보이지 않는다
        popup.OpenAsync().Forget();
    }

    public async UniTask CloseAsync(PopupBase popup)
    {
        if (popup == null || !popup.IsOpen || popup.IsTransitioning)
            return;

        await popup.CloseAsync();

        _openPopups.Remove(popup);
        DisposeScope(popup);
    }

    public void ManagedUpdate()
    {
        if (_openPopups.Count == 0)
            return;

        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
            return;

        var top = _openPopups[_openPopups.Count - 1];
        if (top == null || top.IsTransitioning)
            return;

        top.HandleBackPressed();
    }

    private async UniTask<PopupBase> GetOrCreateAsync(string key)
    {
        if (_instances.TryGetValue(key, out var cached) && cached != null)
            return cached;

        if (_container == null)
        {
            Debug.Log("[PopupService] 팝업 부모(PopupRoot)가 없어 팝업을 열 수 없습니다.");
            return null;
        }

        var prefab = await _resourceLoadService.LoadAsync<GameObject>(key);
        if (prefab == null)
        {
            Debug.Log($"[PopupService] 팝업 프리팹 로드 실패: {key}");
            return null;
        }

        if (_disposed)
            return null;

        // 같은 키를 동시에 열면 로드를 기다리는 사이 먼저 끝난 쪽이 이미 생성했을 수 있다
        if (_instances.TryGetValue(key, out cached) && cached != null)
            return cached;

        var instance = _resolver.Instantiate(prefab, _container);
        var popup = instance.GetComponent<PopupBase>();
        if (popup == null)
        {
            Debug.Log($"[PopupService] {key} 프리팹 루트에 PopupBase가 없습니다.", instance);
            Object.Destroy(instance);
            return null;
        }

        instance.SetActive(false);
        popup.CloseRequested += HandleCloseRequested;
        _instances[key] = popup;

        return popup;
    }

    private void CreateScope(PopupBase popup)
    {
        var attribute = popup.GetType().GetCustomAttribute<PopupPresenterAttribute>();
        if (attribute == null)
            return;

        // 팝업이 열려 있는 동안만 Presenter가 살아있도록 자식 스코프에서 해결한다
        var scope = _resolver.CreateScope(builder =>
        {
            builder.RegisterInstance(popup, popup.GetType());
            builder.Register(attribute.PresenterType, Lifetime.Scoped).As(typeof(IPopupPresenter));
        });

        _scopes[popup] = scope;
        scope.Resolve<IPopupPresenter>().Initialize();
    }

    private void DisposeScope(PopupBase popup)
    {
        if (!_scopes.Remove(popup, out var scope))
            return;

        scope.Dispose();
    }

    private void HandleCloseRequested(PopupBase popup)
    {
        CloseAsync(popup).Forget();
    }

    public void Dispose()
    {
        _disposed = true;
        _updateSubscriptionService.UnregisterUpdatable(this);

        foreach (var popup in _instances.Values)
        {
            if (popup != null)
                popup.CloseRequested -= HandleCloseRequested;
        }

        foreach (var scope in _scopes.Values)
            scope.Dispose();

        _scopes.Clear();
        _instances.Clear();
        _openPopups.Clear();
    }
}
