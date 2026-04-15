using System.Collections;
using Data;
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

        public static void LoadAdditive(string sceneName)
        {
            if (IsLoaded(sceneName)) return;
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }

        public static void Unload(string sceneName)
        {
            if (!IsLoaded(sceneName)) return;
            SceneManager.UnloadSceneAsync(sceneName);
        }

        public static void LoadWithLoading(string gameplayScene, string loadingScene)
        {
            EnsureInstance();
            Instance.StartFlow(gameplayScene, loadingScene, null);
        }

        public static void LoadWithLoading(string gameplayScene, string loadingScene, string additiveScene)
        {
            EnsureInstance();
            Instance.StartFlow(gameplayScene, loadingScene, additiveScene);
        }

        void StartFlow(string gameplay, string loading, string additive)
        {
            if (_currentFlow != null)
                StopCoroutine(_currentFlow);

            _currentFlow = StartCoroutine(Flow(gameplay, loading, additive));
        }

        IEnumerator Flow(string gameplayScene, string loadingScene, string additiveScene)
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

                if (!string.IsNullOrEmpty(additiveScene) && !IsLoaded(additiveScene))
                {
                    var additiveOp = SceneManager.LoadSceneAsync(additiveScene, LoadSceneMode.Additive);
                    if (additiveOp != null)
                        yield return additiveOp;
                }

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
