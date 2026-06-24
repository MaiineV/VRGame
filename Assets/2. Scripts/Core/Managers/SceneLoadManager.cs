using System.Collections;
using Data;
using Services;
using Services.Atmosphere;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utilities;

namespace Core.Managers
{
    public sealed class SceneLoadManager : MonoBehaviour
    {
        public static SceneLoadManager Instance { get; private set; }

        private const float DefaultMinLoadingSeconds = 1f;
        private const string DefaultConfigResource = "SceneLoadSO";

        [Header("Config")]
        [SerializeField] private SceneLoadSO so;

        private Coroutine _currentFlow;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static void Load(string sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        public static void LoadWithLoading(string gameplayScene, string loadingScene)
        {
            EnsureInstance();
            Instance.StartFlow(gameplayScene, loadingScene);
        }

        void StartFlow(string gameplay, string loading)
        {
            if (_currentFlow != null)
                StopCoroutine(_currentFlow);

            _currentFlow = StartCoroutine(Flow(gameplay, loading));
        }

        IEnumerator Flow(string gameplayScene, string loadingScene)
        {
            try
            {
                if (!string.IsNullOrEmpty(loadingScene))
                {
                    var loadingOp = SceneManager.LoadSceneAsync(loadingScene, LoadSceneMode.Single);
                    if (loadingOp == null)
                    {
                        MyLogger.LogError($"[SceneLoadManager] Failed to load loading scene '{loadingScene}'.");
                        yield break;
                    }
                    yield return loadingOp;
                }

                float shownAt = Time.realtimeSinceStartup;

                var gameplayOp = SceneManager.LoadSceneAsync(gameplayScene, LoadSceneMode.Additive);
                if (gameplayOp == null)
                {
                    MyLogger.LogError($"[SceneLoadManager] Failed to load gameplay scene '{gameplayScene}'.");
                    yield break;
                }
                gameplayOp.allowSceneActivation = false;

                while (gameplayOp.progress < 0.9f)
                    yield return null;

                float elapsed = Time.realtimeSinceStartup - shownAt;
                float minSeconds = GetConfig().minimumLoadingSeconds;
                if (elapsed < minSeconds)
                    yield return new WaitForSecondsRealtime(minSeconds - elapsed);

                gameplayOp.allowSceneActivation = true;

                while (!gameplayOp.isDone)
                    yield return null;

                var gp = SceneManager.GetSceneByName(gameplayScene);
                if (gp.IsValid())
                    SceneManager.SetActiveScene(gp);

                // Hold the screen black for a beat once gameplay is active, then fade in. The Quest CPU/GPU
                // clocks ramp from a cold-start low (the game opens ~40 FPS and climbs to target over a few
                // seconds); covering that ramp hides the visible judder. Fires while the Loading scene's
                // own black quad is still up, so the handoff to this cover is seamless before it unloads.
                if (ServiceLocator.TryGet<IAtmosphereService>(out var atmosphere))
                    atmosphere.CoverFadeIn(1.5f, 0.6f);

                if (!string.IsNullOrEmpty(loadingScene) && IsLoaded(loadingScene))
                {
                    var unloadLoading = SceneManager.UnloadSceneAsync(loadingScene);
                    if (unloadLoading != null)
                        yield return unloadLoading;
                }

#if !UNITY_EDITOR
                if (GetConfig().unloadUnusedAssetsInPlayer)
                {
                    yield return Resources.UnloadUnusedAssets();
                    System.GC.Collect();
                }
#endif
            }
            finally
            {
                _currentFlow = null;
            }
        }

        SceneLoadSO GetConfig()
        {
            if (so) return so;
            so = Resources.Load<SceneLoadSO>(DefaultConfigResource);

            if (!so)
            {
                so = ScriptableObject.CreateInstance<SceneLoadSO>();
                so.minimumLoadingSeconds = DefaultMinLoadingSeconds;
            }

            return so;
        }

        static bool IsLoaded(string name)
        {
            var scene = SceneManager.GetSceneByName(name);
            return scene.IsValid() && scene.isLoaded;
        }

        static void EnsureInstance()
        {
            if (Instance) return;

            var go = new GameObject("~SceneLoadManager");
            Instance = go.AddComponent<SceneLoadManager>();
            DontDestroyOnLoad(go);
        }
    }
}
