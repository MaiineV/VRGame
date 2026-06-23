using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace Services
{
    public struct ServiceDefinition
    {
        public Type Service { get; }
        public bool IsSceneUnloaded { get;}

        public ServiceDefinition(Type pService, bool pIsSceneUnloaded)
        {
            Service = pService;
            IsSceneUnloaded = pIsSceneUnloaded;
        }
    }
    
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, ServiceDefinition> ServiceDefinitions = new();
        private static readonly Dictionary<Type, IGameService> ServiceInstances = new();
        private static readonly HashSet<Type> InitializingServices = new();
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            SceneManager.sceneUnloaded += SceneManagerOnSceneUnloaded;
        }

        public static void Register<TInterface, TInstance>(bool pIsSceneUnloaded = false, bool mImmediateInit = false)
            where TInterface : IGameService where TInstance : class, TInterface
        {
            var lInterfaceType = typeof(TInterface);
            var lInstanceClassType = typeof(TInstance);

            Assert.IsFalse(ServiceDefinitions.ContainsKey(lInterfaceType),
                $"Service {lInterfaceType} is already registered");
            ServiceDefinitions.Add(lInterfaceType, new ServiceDefinition(lInstanceClassType, pIsSceneUnloaded));

            if (mImmediateInit)
            {
                InitializeService<TInterface>();
            }
        }

        public static T Get<T>() where T : IGameService
        {
            var lType = typeof(T);
            return (T)Get(lType);
        }

        public static bool TryGet<T>(out T service) where T : class, IGameService
        {
            var lType = typeof(T);
            if (!ServiceDefinitions.ContainsKey(lType))
            {
                service = null;
                return false;
            }

            if (ServiceInstances.TryGetValue(lType, out var instance))
            {
                service = instance as T;
                return service != null;
            }

            try
            {
                var initialized = InitializeService(lType);
                service = initialized as T;
                return service != null;
            }
            catch (Exception ex)
            {
                Utilities.MyLogger.LogError($"[ServiceLocator] TryGet failed for {lType}: {ex.Message}");
                service = null;
                return false;
            }
        }

        private static IGameService Get(Type pType)
        {
            return ServiceInstances.TryGetValue(pType, out var lInstance) ? lInstance : InitializeService(pType);
        }

        private static T InitializeService<T>() where T : IGameService
        {
            var lType = typeof(T);
            return (T)InitializeService(lType);
        }

        private static IGameService InitializeService(Type pServiceType)
        {
            if (!ServiceDefinitions.ContainsKey(pServiceType))
                throw new Exception($"Service {pServiceType} not found");

            lock (InitializingServices)
            {
                if (InitializingServices.Contains(pServiceType))
                {
                    Utilities.MyLogger.LogError($"[ServiceLocator] Circular service dependency detected while creating {pServiceType}");
                    throw new Exception($"Circular service dependency detected while creating {pServiceType}");
                }

                InitializingServices.Add(pServiceType);
            }

            try
            {
                var lConcreteType = ServiceDefinitions[pServiceType];

                var lNewInstance = CreateInstance(lConcreteType.Service);
                ServiceInstances.Add(pServiceType, lNewInstance);

                lNewInstance.Initialize();

                return lNewInstance;
            }
            finally
            {
                lock (InitializingServices)
                {
                    InitializingServices.Remove(pServiceType);
                }
            }
        }

        private static bool AreParametersValid(ConstructorInfo pConstructorInfo)
        {
            return pConstructorInfo.GetParameters().All(pP => typeof(IGameService).IsAssignableFrom(pP.ParameterType));
        }

        private static IGameService CreateInstance(Type pConcreteType)
        {
            var lConstructors = pConcreteType.GetConstructors();

            if (lConstructors.Length == 0 || lConstructors[0].GetParameters().Length == 0)
            {
                return (IGameService)Activator.CreateInstance(pConcreteType);
            }

            if (lConstructors.Length > 1 || !AreParametersValid(lConstructors[0]))
            {
                throw new Exception(
                    $"The Service {pConcreteType} can't be created. It should define only one constructor with IGameService parameters or an empty constructor.");
            }

            var lConstructorParameters = lConstructors[0].GetParameters();
            var lInstanceParameters = new object[lConstructorParameters.Length];

            for (var lI = 0; lI < lConstructorParameters.Length; lI++)
            {
                lInstanceParameters[lI] = Get(lConstructorParameters[lI].ParameterType);
            }

            return (IGameService)Activator.CreateInstance(pConcreteType, lInstanceParameters);
        }

        private static void SceneManagerOnSceneUnloaded(Scene pArg0)
        {
            foreach (var lType in ServiceDefinitions.Keys)
            {
                if (!ServiceInstances.ContainsKey(lType))
                    continue;
                
                if (!ServiceDefinitions[lType].IsSceneUnloaded)
                    continue;
                
                if (ServiceInstances[lType] is IDisposable lTarget)
                {
                    lTarget.Dispose();
                }

                ServiceInstances.Remove(lType);
            }
        }
    }

    public interface IGameService
    {
        void Initialize();
    }
}
