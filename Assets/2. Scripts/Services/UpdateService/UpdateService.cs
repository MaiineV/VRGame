using System.Collections.Generic;

namespace Services.UpdateService
{
    public class UpdateService : IUpdateService
    {
        // Ordered list drives iteration; the HashSet gives O(1) membership so Add stays idempotent
        // without an O(n) Contains scan. Both are kept in sync by Add/Remove.
        private List<IUpdateListener> _updateListeners;
        private List<IFixedUpdateListener> _fixedUpdateListeners;
        private List<ILateUpdateListener> _lateUpdateListeners;

        private HashSet<IUpdateListener> _updateSet;
        private HashSet<IFixedUpdateListener> _fixedUpdateSet;
        private HashSet<ILateUpdateListener> _lateUpdateSet;

        // Reusable scratch buffers: we iterate a snapshot so a listener may add/remove itself (or
        // others) mid-tick without shifting the live list under the loop and skipping a tick.
        private readonly List<IUpdateListener> _updateBuffer = new List<IUpdateListener>();
        private readonly List<IFixedUpdateListener> _fixedUpdateBuffer = new List<IFixedUpdateListener>();
        private readonly List<ILateUpdateListener> _lateUpdateBuffer = new List<ILateUpdateListener>();

        public void Initialize()
        {
            _updateListeners = new List<IUpdateListener>();
            _fixedUpdateListeners = new List<IFixedUpdateListener>();
            _lateUpdateListeners = new List<ILateUpdateListener>();

            _updateSet = new HashSet<IUpdateListener>();
            _fixedUpdateSet = new HashSet<IFixedUpdateListener>();
            _lateUpdateSet = new HashSet<ILateUpdateListener>();
        }

        public void AddUpdateListener(IUpdateListener listener)
        {
            if (_updateSet.Add(listener))
                _updateListeners.Add(listener);
        }

        public void AddFixedUpdateListener(IFixedUpdateListener listener)
        {
            if (_fixedUpdateSet.Add(listener))
                _fixedUpdateListeners.Add(listener);
        }

        public void AddLateUpdateListener(ILateUpdateListener listener)
        {
            if (_lateUpdateSet.Add(listener))
                _lateUpdateListeners.Add(listener);
        }

        public void RemoveUpdateListener(IUpdateListener listener)
        {
            if (_updateSet.Remove(listener))
                _updateListeners.Remove(listener);
        }

        public void RemoveFixedUpdateListener(IFixedUpdateListener listener)
        {
            if (_fixedUpdateSet.Remove(listener))
                _fixedUpdateListeners.Remove(listener);
        }

        public void RemoveLateUpdateListener(ILateUpdateListener listener)
        {
            if (_lateUpdateSet.Remove(listener))
                _lateUpdateListeners.Remove(listener);
        }

        public void MyUpdate()
        {
            _updateBuffer.Clear();
            _updateBuffer.AddRange(_updateListeners);
            for (var i = 0; i < _updateBuffer.Count; i++)
                _updateBuffer[i].MyUpdate();
        }

        public void MyFixedUpdate()
        {
            _fixedUpdateBuffer.Clear();
            _fixedUpdateBuffer.AddRange(_fixedUpdateListeners);
            for (var i = 0; i < _fixedUpdateBuffer.Count; i++)
                _fixedUpdateBuffer[i].MyFixedUpdate();
        }

        public void MyLateUpdate()
        {
            _lateUpdateBuffer.Clear();
            _lateUpdateBuffer.AddRange(_lateUpdateListeners);
            for (var i = 0; i < _lateUpdateBuffer.Count; i++)
                _lateUpdateBuffer[i].MyLateUpdate();
        }
    }
}
