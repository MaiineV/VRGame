using Gameplay.Systems;
using Services;
using Services.Audio;
using Services.Database;
using Services.Economy;
using Services.GameState;
using Services.Night;
using Services.Progression;
using Services.Recipe;
using Services.Save;
using Services.UpdateService;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core.Managers;
using Utilities;

namespace Core
{
    public class GameBootstrap : MonoBehaviour
    {
        private static GameBootstrap _instance;

        [Header("Scene Settings")]
        [SerializeField] private string firstSceneName = "Bar";

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
            ServiceLocator.Register<IRecipeService, RecipeService>(mImmediateInit: true);
            ServiceLocator.Register<IEconomyService, EconomyService>(mImmediateInit: true);
            ServiceLocator.Register<IProgressionService, ProgressionService>(mImmediateInit: true);
            ServiceLocator.Register<IBreakablePoolService, BreakablePoolService>(mImmediateInit: true);
            ServiceLocator.Register<ICustomerPoolService, CustomerPoolService>(mImmediateInit: true);
            ServiceLocator.Register<IGlassPoolService, GlassPoolService>(mImmediateInit: true);
            ServiceLocator.Register<IAudioService, AudioService>(mImmediateInit: true);
            ServiceLocator.Register<INightService, NightService>(mImmediateInit: true);
            ServiceLocator.Register<IGameStateService, GameStateService>(mImmediateInit: true);
            // TODO: registrar progresivamente segun se implementen
            //   IUIService
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
