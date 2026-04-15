using UnityEngine;
using Utilities;

namespace Core.Managers
{
    
    public abstract class BaseManager : MonoBehaviour, IManager
    {
        protected bool _isInitialized;

        public bool IsInitialized => _isInitialized;
    
        public virtual void Initialize()
        {
            if (_isInitialized)
            {

                MyLogger.LogWarning($"{GetType().Name} is already initialized!");
                return;
            }

            OnInitialize();
            _isInitialized = true;
        }
    
        public virtual void Shutdown()
        {
            if (!_isInitialized)
            {
                return;
            }

            OnShutdown();
            _isInitialized = false;
        }
    
        protected abstract void OnInitialize();
        protected virtual void OnShutdown() { }
    
        protected virtual void OnDestroy()
        {
            Shutdown();
        }
    }
}