#if UNITY_EDITOR
using Data.SO;
using Gameplay.Interactions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EditorTools
{
    public static class BottleWirer
    {
        [MenuItem("Pour Decisions/Visuals/Wire Bottle SOs")]
        public static void Wire()
        {
            var guids = AssetDatabase.FindAssets("t:BottleSO", new[] { "Assets/Resources/Database/Bottles" });
            var bottleSOs = new BottleSO[guids.Length];
            for (int i = 0; i < guids.Length; i++)
                bottleSOs[i] = AssetDatabase.LoadAssetAtPath<BottleSO>(AssetDatabase.GUIDToAssetPath(guids[i]));

            var root = GameObject.Find("__BarSceneRoot");
            if (root == null) { Debug.LogError("[BottleWirer] __BarSceneRoot not found."); return; }
            var shelf = root.transform.Find("BottleShelf");
            if (shelf == null) { Debug.LogError("[BottleWirer] BottleShelf not found."); return; }

            // Remove duplicate Bottle components
            for (int s = 0; s < shelf.childCount; s++)
            {
                var slot = shelf.GetChild(s);
                for (int b = 0; b < slot.childCount; b++)
                {
                    var bottleT = slot.GetChild(b);
                    var allBottle = bottleT.GetComponents<Bottle>();
                    if (allBottle.Length <= 1) continue;
                    Bottle keep = null;
                    foreach (var c in allBottle)
                    {
                        var cso = new SerializedObject(c);
                        var neckProp = cso.FindProperty("_neck");
                        if (neckProp != null && neckProp.objectReferenceValue != null)
                        {
                            keep = c;
                            break;
                        }
                    }
                    if (keep == null) keep = allBottle[0];
                    foreach (var c in allBottle)
                        if (c != keep) Object.DestroyImmediate(c);
                }
            }

            // Wire BottleSOs
            int wired = 0;
            int bottleIdx = 0;
            for (int s = 0; s < shelf.childCount; s++)
            {
                var slot = shelf.GetChild(s);
                for (int b = 0; b < slot.childCount; b++)
                {
                    var bottle = slot.GetChild(b).GetComponent<Bottle>();
                    if (bottle == null) continue;

                    var so = new SerializedObject(bottle);
                    var prop = so.FindProperty("_so");
                    if (prop == null) continue;
                    if (prop.objectReferenceValue != null) { bottleIdx++; continue; }

                    prop.objectReferenceValue = bottleSOs[bottleIdx % bottleSOs.Length];
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(bottle);
                    wired++;
                    bottleIdx++;
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[BottleWirer] Wired {wired} bottles with BottleSO assets.");
        }
    }
}
#endif
