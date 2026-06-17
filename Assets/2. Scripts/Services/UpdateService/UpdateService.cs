using System.Collections.Generic;

namespace Services.UpdateService
{
    public class UpdateService : IUpdateService
    {
        private List<IUpdateListener> _updateListeners;
        private List<IFixedUpdateListener> _fixedUpdateListeners;
        private List<ILateUpdateListener> _lateUpdateListeners;

        public void Initialize()
        {
            _updateListeners = new List<IUpdateListener>();
            _fixedUpdateListeners = new List<IFixedUpdateListener>();
            _lateUpdateListeners = new List<ILateUpdateListener>();
        }

        public void AddUpdateListener(IUpdateListener listener)
        {
            if (!_updateListeners.Contains(listener))
                _updateListeners.Add(listener);
        }

        public void AddFixedUpdateListener(IFixedUpdateListener listener)
        {
            if (!_fixedUpdateListeners.Contains(listener))
                _fixedUpdateListeners.Add(listener);
        }

        public void AddLateUpdateListener(ILateUpdateListener listener)
        {
            if (!_lateUpdateListeners.Contains(listener))
                _lateUpdateListeners.Add(listener);
        }

        public void RemoveUpdateListener(IUpdateListener listener) => _updateListeners.Remove(listener);
        public void RemoveFixedUpdateListener(IFixedUpdateListener listener) => _fixedUpdateListeners.Remove(listener);
        public void RemoveLateUpdateListener(ILateUpdateListener listener) => _lateUpdateListeners.Remove(listener);

        public void MyUpdate()
        {
            for (var i = 0; i < _updateListeners.Count; i++)
                _updateListeners[i].MyUpdate();
        }

        public void MyFixedUpdate()
        {
            for (var i = 0; i < _fixedUpdateListeners.Count; i++)
                _fixedUpdateListeners[i].MyFixedUpdate();
        }

        public void MyLateUpdate()
        {
            for (var i = 0; i < _lateUpdateListeners.Count; i++)
                _lateUpdateListeners[i].MyLateUpdate();
        }
    }
}
