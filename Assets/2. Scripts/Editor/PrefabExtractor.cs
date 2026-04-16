#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    public static class PrefabExtractor
    {
        private const string TargetFolder = "Assets/4. Prefabs";

        [MenuItem("Pour Decisions/Prefabs/Extract Scene to Prefabs")]
        public static void Extract()
        {
            if (!Directory.Exists(TargetFolder))
                Directory.CreateDirectory(TargetFolder);

            var root = GameObject.Find("__BarSceneRoot");
            if (root == null) { Debug.LogError("[PrefabExtractor] __BarSceneRoot not found."); return; }

            int created = 0;

            // --- Seat prefab (from Seat_0 as template) ---
            var seats = root.transform.Find("Seats");
            if (seats != null && seats.childCount > 0)
            {
                var seatTemplate = seats.GetChild(0);
                created += SaveIfNew(seatTemplate.gameObject, "Seat");

                // Connect all seats to the prefab
                var seatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{TargetFolder}/Seat.prefab");
                if (seatPrefab != null)
                {
                    for (int i = 0; i < seats.childCount; i++)
                        ConnectToPrefab(seats.GetChild(i).gameObject, seatPrefab);
                }
            }

            // --- CashRegister prefab ---
            var cashAnchor = root.transform.Find("CashRegisterAnchor");
            if (cashAnchor != null)
            {
                var cash = cashAnchor.Find("CashRegister");
                if (cash != null)
                    created += SaveAndConnect(cash.gameObject, "CashRegister");
            }

            // --- NightClipboard prefab ---
            var clipboard = root.transform.Find("NightClipboard");
            if (clipboard != null)
                created += SaveAndConnect(clipboard.gameObject, "NightClipboard");

            // --- OrdersBoard prefab ---
            var board = root.transform.Find("OrdersBoard");
            if (board != null)
                created += SaveAndConnect(board.gameObject, "OrdersBoard");

            // --- Move existing prefabs from Assets/Prefabs to 4. Prefabs ---
            MoveExistingPrefab("Assets/Prefabs/Bottle.prefab", $"{TargetFolder}/Bottle.prefab");
            MoveExistingPrefab("Assets/Prefabs/Glass.prefab", $"{TargetFolder}/Glass.prefab");
            MoveExistingPrefab("Assets/Prefabs/Customer.prefab", $"{TargetFolder}/Customer.prefab");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[PrefabExtractor] Created {created} prefabs in {TargetFolder}.");
        }

        private static int SaveIfNew(GameObject go, string name)
        {
            var path = $"{TargetFolder}/{name}.prefab";
            if (File.Exists(path)) { Debug.Log($"[PrefabExtractor] {name}.prefab already exists, skipping."); return 0; }
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
            Debug.Log($"[PrefabExtractor] Created {name}.prefab");
            return 1;
        }

        private static int SaveAndConnect(GameObject go, string name)
        {
            var path = $"{TargetFolder}/{name}.prefab";
            if (File.Exists(path)) { Debug.Log($"[PrefabExtractor] {name}.prefab already exists, skipping."); return 0; }
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
            Debug.Log($"[PrefabExtractor] Created {name}.prefab");
            return 1;
        }

        private static void ConnectToPrefab(GameObject instance, GameObject prefab)
        {
            if (PrefabUtility.GetCorrespondingObjectFromSource(instance) == prefab) return;
            var settings = new ConvertToPrefabInstanceSettings
            {
                changeRootNameToAssetName = false,
                objectMatchMode = ObjectMatchMode.ByHierarchy,
                logInfo = false
            };
            PrefabUtility.ConvertToPrefabInstance(instance, prefab, settings, InteractionMode.AutomatedAction);
        }

        private static void MoveExistingPrefab(string from, string to)
        {
            if (!File.Exists(from)) return;
            if (File.Exists(to)) { Debug.Log($"[PrefabExtractor] {Path.GetFileName(to)} already in target."); return; }
            var result = AssetDatabase.MoveAsset(from, to);
            if (string.IsNullOrEmpty(result))
                Debug.Log($"[PrefabExtractor] Moved {Path.GetFileName(from)} → 4. Prefabs/");
            else
                Debug.LogWarning($"[PrefabExtractor] Failed to move {from}: {result}");
        }
    }
}
#endif
