#if UNITY_EDITOR
using Gameplay.Interactions;
using Gameplay.Liquid;
using TMPro;
using UI.Diegetic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EditorTools
{
    public static class GameplayVisualsScaffolder
    {
        [MenuItem("Pour Decisions/Visuals/Scaffold Gameplay Visuals")]
        public static void Scaffold()
        {
            var root = GameObject.Find("__BarSceneRoot");
            if (root == null) { Debug.LogError("[GameplayVisualsScaffolder] __BarSceneRoot not found."); return; }

            int changes = 0;
            changes += AddGlassFillBars(root.transform);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[GameplayVisualsScaffolder] Applied {changes} changes.");
        }

        private static int AddGlassFillBars(Transform root)
        {
            int added = 0;
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                var glass = child.GetComponent<Glass>();
                if (glass == null) continue;
                if (child.GetComponentInChildren<GlassFillBar>(true) != null) continue;

                var barGo = new GameObject("FillBar");
                barGo.transform.SetParent(child, false);
                barGo.transform.localPosition = new Vector3(0, 0.18f, 0);

                var canvas = barGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                var rt = barGo.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0.08f, 0.015f);

                barGo.AddComponent<CanvasScaler>();

                var bgGo = new GameObject("Bg");
                bgGo.transform.SetParent(barGo.transform, false);
                var bgRt = bgGo.AddComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;
                var bgImg = bgGo.AddComponent<Image>();
                bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);

                var fillGo = new GameObject("Fill");
                fillGo.transform.SetParent(barGo.transform, false);
                var fillRt = fillGo.AddComponent<RectTransform>();
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = Vector2.one;
                fillRt.offsetMin = Vector2.zero;
                fillRt.offsetMax = Vector2.zero;
                var fillImg = fillGo.AddComponent<Image>();
                fillImg.color = new Color(0.4f, 0.6f, 1f);
                fillImg.type = Image.Type.Filled;
                fillImg.fillMethod = Image.FillMethod.Horizontal;
                fillImg.fillAmount = 0f;

                var bar = barGo.AddComponent<GlassFillBar>();
                var so = new SerializedObject(bar);
                so.FindProperty("_container").objectReferenceValue = glass;
                so.FindProperty("_fillImage").objectReferenceValue = fillImg;
                so.ApplyModifiedPropertiesWithoutUndo();

                added++;
            }
            return added;
        }
    }
}
#endif
