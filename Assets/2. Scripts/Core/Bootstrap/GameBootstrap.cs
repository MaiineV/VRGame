using Gameplay.Systems;
using Services;
using Services.Audio;
using Services.Database;
using Services.Economy;
using Services.GameState;
using Services.Haptics;
using Services.Night;
using Services.Progression;
using Services.Save;
using Services.UpdateService;
using Services.Vfx;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core.Managers;
using Services.Atmosphere;
using Utilities;

namespace Core
{
    public class GameBootstrap : MonoBehaviour
    {
        private static GameBootstrap _instance;

        [Header("Scene Settings")]
        // Actual value is serialized on the Boot scene's GameBootstrap component — update it there in the Editor too.
        [SerializeField] private string firstSceneName = "MainMenu";

        [Header("Loading Settings")]
        [SerializeField] private string loadingSceneName = "Loading";

        [Header("Safety")]
        [SerializeField] private bool skipIfAlreadyInTargetScene = true;

        private bool _started;
        private IUpdateService _updateService;
        private bool _useFallbackUpdatePump;

        void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            ConfigurePhysicsLayers();

            ServiceLocator.Initialize();
            RegisterServices();
            InitializeCriticalServices();

            MyLogger.LogInfo("=== POUR DECISIONS BOOTSTRAP INITIALIZED ===");
        }

        void OnDestroy()
        {
            // Clear the static so a fresh bootstrap (e.g. a later domain reload or scene reload)
            // isn't permanently short-circuited by a stale reference to a destroyed instance.
            if (_instance == this) _instance = null;
        }

        void Start()
        {
            if (_started) return;
            _started = true;

            _useFallbackUpdatePump = FindFirstObjectByType<UpdateServiceObject>() == null;
            if (_useFallbackUpdatePump)
                MyLogger.LogWarning("[GameBootstrap] UpdateServiceObject not found. Using fallback update pump.");

            LoadFirstScene();
        }

        void Update()
        {
            if (!_useFallbackUpdatePump || _updateService == null) return;
            _updateService.MyUpdate();
        }

        void FixedUpdate()
        {
            if (!_useFallbackUpdatePump || _updateService == null) return;
            _updateService.MyFixedUpdate();
        }

        void LateUpdate()
        {
            if (!_useFallbackUpdatePump || _updateService == null) return;
            _updateService.MyLateUpdate();
        }

        void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            if (ServiceLocator.TryGet<ISaveService>(out var save)) save.Save();
        }

        void OnApplicationQuit()
        {
            if (ServiceLocator.TryGet<ISaveService>(out var save)) save.Save();
        }

        private void RegisterServices()
        {
            ServiceLocator.Register<IUpdateService, UpdateService>();
            ServiceLocator.Register<IDatabaseService, DatabaseService>(mImmediateInit: true);
            ServiceLocator.Register<ISaveService, SaveService>(mImmediateInit: true);
            ServiceLocator.Register<IEconomyService, EconomyService>(mImmediateInit: true);
            ServiceLocator.Register<IProgressionService, ProgressionService>(mImmediateInit: true);
            ServiceLocator.Register<IBreakablePoolService, BreakablePoolService>(mImmediateInit: true);
            ServiceLocator.Register<ICustomerPoolService, CustomerPoolService>(mImmediateInit: true);
            ServiceLocator.Register<IGlassPoolService, GlassPoolService>(mImmediateInit: true);
            ServiceLocator.Register<IAudioService, AudioService>(mImmediateInit: true);
            ServiceLocator.Register<IHapticService, HapticService>(mImmediateInit: true);
            ServiceLocator.Register<IVfxService, VfxService>(mImmediateInit: true);
            ServiceLocator.Register<INightService, NightService>(mImmediateInit: true);
            ServiceLocator.Register<IGameStateService, GameStateService>(mImmediateInit: true);
            // Registered after GameStateService: AtmosphereService subscribes to it in Initialize(),
            // so the state service must already be registered when this immediate-inits.
            ServiceLocator.Register<IAtmosphereService, AtmosphereService>(mImmediateInit: true);
        }

        private static void ConfigurePhysicsLayers()
        {
            int hand = LayerMask.NameToLayer("Hand");
            if (hand >= 0) Physics.IgnoreLayerCollision(hand, hand, true);
        }

        private void InitializeCriticalServices()
        {
            _updateService = ServiceLocator.Get<IUpdateService>();
        }

        void LoadFirstScene()
        {
            var active = SceneManager.GetActiveScene().name;

            if (skipIfAlreadyInTargetScene && active == firstSceneName)
            {
                MyLogger.LogInfo($"Already in target scene: {firstSceneName}. Skipping load.");
                return;
            }

            MyLogger.LogInfo($"Loading first scene: {firstSceneName} (via {loadingSceneName})");

            if (string.IsNullOrEmpty(loadingSceneName))
                SceneLoadManager.Load(firstSceneName);
            else
                SceneLoadManager.LoadWithLoading(firstSceneName, loadingSceneName);
        }
    }
}
