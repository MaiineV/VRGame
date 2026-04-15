#if UNITY_EDITOR
using System.IO;
using Core;
using Core.Managers;
using Gameplay;
using Gameplay.Customer;
using Services.UpdateService;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EditorTools
{
    public static class SceneScaffolder
    {
        private const string ScenesFolder = "Assets/Scenes";
        private const int SeatCount = 3;
        private const float BarLength = 4f;

        [MenuItem("Pour Decisions/Scenes/Scaffold All (Boot + Loading + Bar)")]
        public static void ScaffoldAll()
        {
            EnsureFolder();
            CreateBootScene();
            CreateLoadingScene();
            CreateBarScene();
            AddScenesToBuildSettings();
            EditorUtility.DisplayDialog("Pour Decisions",
                "Boot, Loading y Bar creadas en Assets/Scenes.\n\nAhora: abrí Boot, drag el OVRCameraRig (Meta XR) como hijo del PlayerAnchor en Bar.",
                "OK");
        }

        [MenuItem("Pour Decisions/Scenes/Create Boot")]
        public static void CreateBootMenu() { EnsureFolder(); CreateBootScene(); AddScenesToBuildSettings(); }

        [MenuItem("Pour Decisions/Scenes/Create Loading")]
        public static void CreateLoadingMenu() { EnsureFolder(); CreateLoadingScene(); AddScenesToBuildSettings(); }

        [MenuItem("Pour Decisions/Scenes/Create Bar")]
        public static void CreateBarMenu() { EnsureFolder(); CreateBarScene(); AddScenesToBuildSettings(); }

        private static void EnsureFolder()
        {
            if (!Directory.Exists(ScenesFolder))
                Directory.CreateDirectory(ScenesFolder);
        }

        private static void CreateBootScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootstrapGO = new GameObject("GameBootstrap");
            bootstrapGO.AddComponent<GameBootstrap>();

            var updatePump = new GameObject("UpdateServiceObject");
            updatePump.AddComponent<UpdateServiceObject>();

            var sceneLoader = new GameObject("SceneLoadManager");
            sceneLoader.AddComponent<SceneLoadManager>();

            SaveScene(scene, SceneNames.Boot);
        }

        private static void CreateLoadingScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("LoadingCamera");
            var camera = cam.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.nearClipPlane = 0.01f;

            var placeholder = GameObject.CreatePrimitive(PrimitiveType.Quad);
            placeholder.name = "LoadingFadeQuad";
            placeholder.transform.SetParent(cam.transform, false);
            placeholder.transform.localPosition = new Vector3(0, 0, 0.3f);
            placeholder.transform.localScale = Vector3.one * 0.4f;

            SaveScene(scene, SceneNames.Loading);
        }

        private static void CreateBarScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Root
            var root = new GameObject("__BarSceneRoot");
            var barRoot = root.AddComponent<BarSceneRoot>();

            // Player anchor (behind the bar)
            var playerAnchor = new GameObject("PlayerAnchor");
            playerAnchor.transform.SetParent(root.transform);
            playerAnchor.transform.position = new Vector3(0, 0, -0.75f);

            // Bar counter (placeholder cube, scaled)
            var counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "BarCounter";
            counter.transform.SetParent(root.transform);
            counter.transform.position = new Vector3(0, 1f, 0.25f);
            counter.transform.localScale = new Vector3(BarLength, 1f, 0.5f);

            // Seats on the customer side
            var seatsParent = new GameObject("Seats").transform;
            seatsParent.SetParent(root.transform);
            var seats = new CustomerSeatPoint[SeatCount];
            float step = BarLength / (SeatCount + 1);
            for (int i = 0; i < SeatCount; i++)
            {
                var seatGO = new GameObject($"Seat_{i}");
                seatGO.transform.SetParent(seatsParent);
                seatGO.transform.position = new Vector3(-BarLength / 2 + step * (i + 1), 0, 1.25f);

                var serve = new GameObject("ServePoint");
                serve.transform.SetParent(seatGO.transform);
                serve.transform.localPosition = new Vector3(0, 1.1f, -0.7f);

                var lookAt = new GameObject("LookAtPoint");
                lookAt.transform.SetParent(seatGO.transform);
                lookAt.transform.localPosition = new Vector3(0, 1.6f, 0);

                var seat = seatGO.AddComponent<CustomerSeatPoint>();
                SerializePrivateField(seat, "_index", i);
                SerializePrivateField(seat, "_servePoint", serve.transform);
                SerializePrivateField(seat, "_lookAtPoint", lookAt.transform);
                seats[i] = seat;
            }

            // Customer spawn / exit
            var spawn = new GameObject("CustomerSpawnPoint");
            spawn.transform.SetParent(root.transform);
            spawn.transform.position = new Vector3(-BarLength, 0, 3f);

            var exit = new GameObject("CustomerExitPoint");
            exit.transform.SetParent(root.transform);
            exit.transform.position = new Vector3(BarLength, 0, 3f);

            // Bottle shelf (behind bar)
            var shelfParent = new GameObject("BottleShelf").transform;
            shelfParent.SetParent(root.transform);
            shelfParent.position = new Vector3(0, 1.3f, -1.5f);
            var shelfPoints = new Transform[6];
            for (int i = 0; i < 6; i++)
            {
                var p = new GameObject($"ShelfSlot_{i}");
                p.transform.SetParent(shelfParent);
                p.transform.localPosition = new Vector3(-1.5f + i * 0.6f, 0, 0);
                shelfPoints[i] = p.transform;
            }

            // Cash register
            var cash = new GameObject("CashRegisterAnchor");
            cash.transform.SetParent(root.transform);
            cash.transform.position = new Vector3(BarLength / 2 - 0.4f, 1.15f, -0.2f);

            // UpdateServiceObject
            var updatePump = new GameObject("UpdateServiceObject");
            updatePump.AddComponent<UpdateServiceObject>();

            // Wire BarSceneRoot refs
            SerializePrivateField(barRoot, "_playerAnchor", playerAnchor.transform);
            SerializePrivateField(barRoot, "_seats", seats);
            SerializePrivateField(barRoot, "_customerSpawnPoint", spawn.transform);
            SerializePrivateField(barRoot, "_customerExitPoint", exit.transform);
            SerializePrivateField(barRoot, "_bottleShelfPoints", shelfPoints);
            SerializePrivateField(barRoot, "_cashRegisterAnchor", cash.transform);

            SaveScene(scene, SceneNames.Bar);
        }

        private static void SaveScene(UnityEngine.SceneManagement.Scene scene, string name)
        {
            var path = $"{ScenesFolder}/{name}.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[SceneScaffolder] Saved {path}");
        }

        private static void AddScenesToBuildSettings()
        {
            string[] names = { SceneNames.Boot, SceneNames.Loading, SceneNames.Bar };
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (var n in names)
            {
                var path = $"{ScenesFolder}/{n}.unity";
                if (File.Exists(path))
                    list.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = list.ToArray();
        }

        private static void SerializePrivateField(Object target, string fieldName, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null) { Debug.LogError($"Field {fieldName} not found on {target.GetType().Name}"); return; }

            if (value is int i)
            {
                prop.intValue = i;
            }
            else if (value is System.Array rawArr && value is not Object)
            {
                prop.arraySize = rawArr.Length;
                for (int k = 0; k < rawArr.Length; k++)
                    prop.GetArrayElementAtIndex(k).objectReferenceValue = rawArr.GetValue(k) as Object;
            }
            else if (value is Object obj)
            {
                prop.objectReferenceValue = obj;
            }
            else
            {
                Debug.LogError($"Unsupported type for {fieldName}: {value?.GetType()}");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
