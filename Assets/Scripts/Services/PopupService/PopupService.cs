using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Services.UpdateService;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Services.PopupService
{
    public class PopupService : IPopupService, IStartable, IUpdatable, IDisposable
    {
        private readonly IObjectResolver _resolver;
        private readonly IResourceLoadService _resourceLoadService;
        private readonly IUpdateSubscriptionService _updateSubscriptionService;
        private readonly PopupRoot _root;

        private readonly Dictionary<string, PopupBase> _instances = new();
        private readonly List<PopupBase> _openPopups = new();

        private bool _disposed;

        public bool HasOpenPopup => _openPopups.Count > 0;

        public PopupService(
            IObjectResolver resolver,
            IResourceLoadService resourceLoadService,
            IUpdateSubscriptionService updateSubscriptionService,
            PopupRoot root)
        {
            _resolver = resolver;
            _resourceLoadService = resourceLoadService;
            _updateSubscriptionService = updateSubscriptionService;
            _root = root;
        }

        public void Start()
        {
            _updateSubscriptionService.RegisterUpdatable(this);
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

            if (popup.IsOpen)
                return typed;

            popup.transform.SetAsLastSibling();
            _openPopups.Add(popup);

            // 연출 완료를 기다리지 않고 반환해야 호출자가 같은 프레임에 내용을 채워 빈 팝업이 보이지 않는다
            popup.OpenAsync().Forget();
            RefreshDim();

            return typed;
        }

        public async UniTask CloseAsync(PopupBase popup)
        {
            if (popup == null || !popup.IsOpen || popup.IsTransitioning)
                return;

            await popup.CloseAsync();

            _openPopups.Remove(popup);
            RefreshDim();
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

            if (_root == null || _root.Container == null)
            {
                Debug.Log("[PopupService] PopupRoot가 없어 팝업을 열 수 없습니다.");
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

            var instance = _resolver.Instantiate(prefab, _root.Container);
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

        private void HandleCloseRequested(PopupBase popup)
        {
            CloseAsync(popup).Forget();
        }

        private void RefreshDim()
        {
            if (_root == null)
                return;

            for (int i = _openPopups.Count - 1; i >= 0; i--)
            {
                var popup = _openPopups[i];
                if (popup == null || !popup.UseDim)
                    continue;

                _root.ShowDimBelow(popup.transform);
                return;
            }

            _root.HideDim();
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

            _instances.Clear();
            _openPopups.Clear();
        }
    }
}
