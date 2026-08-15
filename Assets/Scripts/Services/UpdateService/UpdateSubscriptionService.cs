using System.Collections.Generic;
using UnityEngine;

namespace Services.UpdateService
{
    public class UpdateSubscriptionService : MonoBehaviour, IUpdateSubscriptionService
    {
        #region Fields

        // Update: 역방향 반복으로 mid-iteration 등록/해제 안전
        private static readonly List<IUpdatable> _updateObservers = new List<IUpdatable>();
        private static int _currentUpdateIndex = -1;

        // LateUpdate/FixedUpdate: pending 패턴으로 다음 프레임에 변경 적용
        private static readonly List<ILateUpdatable> _lateUpdateObservers = new List<ILateUpdatable>();
        private static readonly List<ILateUpdatable> _pendingAddLateUpdateObservers = new List<ILateUpdatable>();
        private static readonly List<ILateUpdatable> _pendingRemoveLateUpdateObservers = new List<ILateUpdatable>();

        private static readonly List<IFixedUpdatable> _fixedUpdateObservers = new List<IFixedUpdatable>();
        private static readonly List<IFixedUpdatable> _pendingAddFixedUpdateObservers = new List<IFixedUpdatable>();
        private static readonly List<IFixedUpdatable> _pendingRemoveFixedUpdateObservers = new List<IFixedUpdatable>();

        private static readonly List<PeriodicSubscriber> _periodicSubscribers = new List<PeriodicSubscriber>();

        #endregion

        #region Inner Classes

        private class PeriodicSubscriber
        {
            public IPeriodicUpdatable Target;
            public float Interval;
            public float Timer;
            public float LastCallTime;
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            for (_currentUpdateIndex = _updateObservers.Count - 1;
                 _currentUpdateIndex >= 0;
                 _currentUpdateIndex--)
            {
                _updateObservers[_currentUpdateIndex].ManagedUpdate();
            }

            HandlePeriodicUpdates();
        }

        private void LateUpdate()
        {
            if (_pendingAddLateUpdateObservers.Count > 0)
            {
                _lateUpdateObservers.AddRange(_pendingAddLateUpdateObservers);
                _pendingAddLateUpdateObservers.Clear();
            }

            if (_pendingRemoveLateUpdateObservers.Count > 0)
            {
                foreach (var item in _pendingRemoveLateUpdateObservers)
                {
                    _lateUpdateObservers.Remove(item);
                }
                _pendingRemoveLateUpdateObservers.Clear();
            }

            foreach (var observer in _lateUpdateObservers)
            {
                observer.ManagedLateUpdate();
            }
        }

        private void FixedUpdate()
        {
            if (_pendingAddFixedUpdateObservers.Count > 0)
            {
                _fixedUpdateObservers.AddRange(_pendingAddFixedUpdateObservers);
                _pendingAddFixedUpdateObservers.Clear();
            }

            if (_pendingRemoveFixedUpdateObservers.Count > 0)
            {
                foreach (var item in _pendingRemoveFixedUpdateObservers)
                {
                    _fixedUpdateObservers.Remove(item);
                }
                _pendingRemoveFixedUpdateObservers.Clear();
            }

            foreach (var observer in _fixedUpdateObservers)
            {
                observer.ManagedFixedUpdate();
            }
        }

        #endregion

        #region Periodic Updates

        private void HandlePeriodicUpdates()
        {
            for (int i = _periodicSubscribers.Count - 1; i >= 0; i--)
            {
                var subscriber = _periodicSubscribers[i];
                subscriber.Timer += Time.deltaTime;

                if (subscriber.Timer >= subscriber.Interval)
                {
                    float deltaTime = subscriber.Timer - subscriber.LastCallTime;
                    subscriber.Target.ManagedPeriodicUpdate(deltaTime);

                    subscriber.Timer -= subscriber.Interval;
                    subscriber.LastCallTime = Time.time;
                }
            }
        }

        #endregion

        #region IUpdateSubscriptionService Implementation

        public void RegisterUpdatable(IUpdatable updatable)
        {
            if (updatable == null || _updateObservers.Contains(updatable))
                return;

            // 반복 중 등록 시 앞에 삽입하고 인덱스 보정
            if (_currentUpdateIndex >= 0)
            {
                _updateObservers.Insert(0, updatable);
                _currentUpdateIndex++;
            }
            else
            {
                _updateObservers.Add(updatable);
            }
        }

        public void UnregisterUpdatable(IUpdatable updatable)
        {
            if (updatable == null)
                return;

            int index = _updateObservers.IndexOf(updatable);
            if (index < 0)
                return;

            _updateObservers.RemoveAt(index);

            // 반복 중 현재 위치 이전 항목 제거 시 인덱스 보정
            if (_currentUpdateIndex >= 0 && index <= _currentUpdateIndex)
            {
                _currentUpdateIndex--;
            }
        }

        public void RegisterLateUpdatable(ILateUpdatable lateUpdatable)
        {
            if (lateUpdatable == null || _lateUpdateObservers.Contains(lateUpdatable) ||
                _pendingAddLateUpdateObservers.Contains(lateUpdatable))
                return;

            _pendingAddLateUpdateObservers.Add(lateUpdatable);
        }

        public void UnregisterLateUpdatable(ILateUpdatable lateUpdatable)
        {
            if (lateUpdatable == null)
                return;

            if (_pendingAddLateUpdateObservers.Contains(lateUpdatable))
            {
                _pendingAddLateUpdateObservers.Remove(lateUpdatable);
                return;
            }

            if (_lateUpdateObservers.Contains(lateUpdatable) &&
                !_pendingRemoveLateUpdateObservers.Contains(lateUpdatable))
            {
                _pendingRemoveLateUpdateObservers.Add(lateUpdatable);
            }
        }

        public void RegisterFixedUpdatable(IFixedUpdatable fixedUpdatable)
        {
            if (fixedUpdatable == null || _fixedUpdateObservers.Contains(fixedUpdatable) ||
                _pendingAddFixedUpdateObservers.Contains(fixedUpdatable))
                return;

            _pendingAddFixedUpdateObservers.Add(fixedUpdatable);
        }

        public void UnregisterFixedUpdatable(IFixedUpdatable fixedUpdatable)
        {
            if (fixedUpdatable == null)
                return;

            if (_pendingAddFixedUpdateObservers.Contains(fixedUpdatable))
            {
                _pendingAddFixedUpdateObservers.Remove(fixedUpdatable);
                return;
            }

            if (_fixedUpdateObservers.Contains(fixedUpdatable) &&
                !_pendingRemoveFixedUpdateObservers.Contains(fixedUpdatable))
            {
                _pendingRemoveFixedUpdateObservers.Add(fixedUpdatable);
            }
        }

        public void RegisterPeriodicUpdatable(IPeriodicUpdatable periodicUpdatable, float interval)
        {
            if (periodicUpdatable == null || interval <= 0)
                return;

            foreach (var subscriber in _periodicSubscribers)
            {
                if (subscriber.Target == periodicUpdatable)
                    return;
            }

            _periodicSubscribers.Add(new PeriodicSubscriber
            {
                Target = periodicUpdatable,
                Interval = interval,
                Timer = 0f,
                LastCallTime = Time.time
            });
        }

        public void UnregisterPeriodicUpdatable(IPeriodicUpdatable periodicUpdatable)
        {
            if (periodicUpdatable == null)
                return;

            for (int i = _periodicSubscribers.Count - 1; i >= 0; i--)
            {
                if (_periodicSubscribers[i].Target == periodicUpdatable)
                {
                    _periodicSubscribers.RemoveAt(i);
                    break;
                }
            }
        }

        #endregion
    }
}
