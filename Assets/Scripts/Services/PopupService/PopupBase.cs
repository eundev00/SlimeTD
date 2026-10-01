using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Services.PopupService
{
    public abstract class PopupBase : MonoBehaviour
    {
        private static readonly int OpenState = Animator.StringToHash("Open");
        private static readonly int CloseState = Animator.StringToHash("Close");

        [SerializeField] private Animator _animator;
        [SerializeField] private bool _useDim = true;

        public event Action<PopupBase> CloseRequested;

        public bool IsOpen { get; private set; }
        public bool IsTransitioning { get; private set; }
        public bool UseDim => _useDim;

        internal async UniTask OpenAsync()
        {
            IsOpen = true;
            IsTransitioning = true;

            gameObject.SetActive(true);
            OnOpen();

            await PlayAsync(OpenState);

            IsTransitioning = false;
        }

        internal async UniTask CloseAsync()
        {
            IsTransitioning = true;

            await PlayAsync(CloseState);

            OnClose();
            gameObject.SetActive(false);

            IsOpen = false;
            IsTransitioning = false;
        }

        internal void HandleBackPressed()
        {
            OnBackPressed();
        }

        public void RequestClose()
        {
            if (!IsOpen || IsTransitioning)
                return;

            CloseRequested?.Invoke(this);
        }

        protected virtual void OnOpen()
        {
        }

        protected virtual void OnClose()
        {
        }

        protected virtual void OnBackPressed()
        {
            RequestClose();
        }

        private async UniTask PlayAsync(int stateHash)
        {
            if (_animator == null || _animator.runtimeAnimatorController == null)
                return;

            if (!_animator.HasState(0, stateHash))
            {
                Debug.Log("[PopupBase] Animator에 Open/Close 상태가 없어 연출을 건너뜁니다.", this);
                return;
            }

            // UI Animator는 정지 상태에서도 매 프레임 값을 써서 Canvas를 다시 그리므로 연출 중에만 켠다
            _animator.enabled = true;
            _animator.Play(stateHash, 0, 0f);
            // 활성화 직후 한 프레임 동안 연출 전 모습이 보이지 않도록 즉시 평가한다
            _animator.Update(0f);

            await UniTask.WaitUntil(
                () => _animator == null || IsStateFinished(stateHash),
                cancellationToken: destroyCancellationToken);

            if (_animator != null)
                _animator.enabled = false;
        }

        private bool IsStateFinished(int stateHash)
        {
            if (_animator.IsInTransition(0))
                return false;

            var info = _animator.GetCurrentAnimatorStateInfo(0);
            return info.shortNameHash == stateHash && info.normalizedTime >= 1f;
        }
    }
}
