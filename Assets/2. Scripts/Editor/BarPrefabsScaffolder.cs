#if UNITY_EDITOR
using System.IO;
using Core;
using Data.Enums;
using Data.SO;
using Gameplay;
using Gameplay.CashRegister;
using Gameplay.Interactions;
using Gameplay.Liquid;
using Gameplay.Systems;
using TMPro;
using UI.Diegetic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EditorTools
{
    public static class BarPrefabsScaffolder
    {
        private const string PrefabsFolder = "Assets/Prefabs";
        private const string BarScenePath = "Assets/Scenes/Bar.unity";
        private const string NightConfigPath = "Assets/Resources/Database/Night/NightConfig_MVP.asset";

        [MenuItem("Pour Decisions/Scenes/Scaffold Bar Prefabs")]
        public static void ScaffoldBarPrefabs()
        {
            if (!Directory.Exists(PrefabsFolder)) Directory.CreateDirectory(PrefabsFolder);

            var glass = CreateGlassPrefab();
            var bottle = CreateBottlePrefab();

            if (!EditorSceneManager.GetActiveScene().path.EndsWith("Bar.unity"))
                EditorSceneManager.OpenScene(BarScenePath, OpenSceneMode.Single);

            PlaceCashRegister();
            PlaceNightClipboard();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[BarPrefabsScaffolder] Prefabs + escena Bar listos.");
        }

        private static GameObject CreateGlassPrefab()
        {
            var path = PrefabsFolder + "/Glass.prefab";
            var root = new GameObject("Glass");

            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.07f, 0.06f, 0.07f);
            Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());

            var col = root.AddComponent<CapsuleCollider>();
            col.height = 0.12f; col.radius = 0.035f; col.direction = 1;
            col.center = new Vector3(0, 0.06f, 0);

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.3f; rb.linearDamping = 0.5f;

            root.AddComponent<GrabBridge>();

            var liquidMeshGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            liquidMeshGO.name = "LiquidMesh";
            liquidMeshGO.transform.SetParent(root.transform, false);
            liquidMeshGO.transform.localScale = new Vector3(0.06f, 0.05f, 0.06f);
            liquidMeshGO.transform.localPosition = new Vector3(0, 0.055f, 0);
            Object.DestroyImmediate(liquidMeshGO.GetComponent<CapsuleCollider>());

            var liquidRenderer = root.AddComponent<LiquidRenderer>();
            TrySetField(liquidRenderer, "_palette", LoadAsset<IngredientPalette>("Assets/Resources/Database/IngredientPalette.asset"));

            var wobble = root.AddComponent<LiquidWobble>();
            TrySetField(wobble, "_renderer", liquidRenderer);
            TrySetField(wobble, "_grab", root.GetComponent<GrabBridge>());

            var breakable = root.AddComponent<Breakable>();
            TrySetEnum(breakable, "_breakSfx", SfxId.GlassBreak);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateBottlePrefab()
        {
            var path = PrefabsFolder + "/Bottle.prefab";
            var root = new GameObject("Bottle");

            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.08f, 0.15f, 0.08f);
            Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());

            var col = root.AddComponent<CapsuleCollider>();
            col.height = 0.3f; col.radius = 0.04f; col.direction = 1;
            col.center = new Vector3(0, 0.15f, 0);

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.5f; rb.linearDamping = 0.5f;

            root.AddComponent<GrabBridge>();

            var neck = new GameObject("Neck").transform;
            neck.SetParent(root.transform, false);
            neck.localPosition = new Vector3(0, 0.3f, 0);

            root.AddComponent<PourDetector>();

            var breakable = root.AddComponent<Breakable>();
            TrySetEnum(breakable, "_breakSfx", SfxId.BottleBreak);

            var bottle = root.AddComponent<Bottle>();
            TrySetField(bottle, "_neck", neck);
            TrySetField(bottle, "_breakable", breakable);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void PlaceCashRegister()
        {
            var anchor = GameObject.Find("__BarSceneRoot/CashRegisterAnchor");
            if (anchor == null) { Debug.LogError("CashRegisterAnchor no existe"); return; }
            if (anchor.transform.Find("CashRegister") != null) return;

            var cash = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cash.name = "CashRegister";
            cash.transform.SetParent(anchor.transform, false);
            cash.transform.localScale = new Vector3(0.25f, 0.15f, 0.2f);
            cash.AddComponent<CashRegister>();

            var labelGO = new GameObject("CashLabel");
            labelGO.transform.SetParent(cash.transform, false);
            labelGO.transform.localPosition = new Vector3(0, 0.6f, -0.55f);
            labelGO.transform.localScale = Vector3.one * 0.02f;
            var cashLabel = labelGO.AddComponent<TextMeshPro>();
            cashLabel.text = "$ 0";
            cashLabel.fontSize = 8;
            cashLabel.alignment = TextAlignmentOptions.Center;

            TrySetField(cash.GetComponent<CashRegister>(), "_cashLabel", cashLabel);
        }

        private static void PlaceNightClipboard()
        {
            var barRoot = GameObject.Find("__BarSceneRoot");
            if (barRoot == null) return;
            if (barRoot.transform.Find("NightClipboard") != null) return;

            var clip = new GameObject("NightClipboard");
            clip.transform.SetParent(barRoot.transform, false);
            clip.transform.position = new Vector3(-1.2f, 1.25f, 0.1f);

            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.name = "ClipboardMesh";
            mesh.transform.SetParent(clip.transform, false);
            mesh.transform.localScale = new Vector3(0.3f, 0.4f, 0.02f);
            Object.DestroyImmediate(mesh.GetComponent<BoxCollider>());

            var col = clip.AddComponent<BoxCollider>();
            col.size = new Vector3(0.3f, 0.4f, 0.02f);

            var rb = clip.AddComponent<Rigidbody>();
            rb.mass = 0.2f; rb.useGravity = false; rb.isKinematic = true;

            var grab = clip.AddComponent<GrabBridge>();

            // Three groups with buttons + labels
            var idle = BuildIdleGroup(clip.transform, out var startBtn, out var idleNight, out var idleBest, out var idleCash);
            var running = BuildRunningGroup(clip.transform, out var abortBtn);
            var summary = BuildSummaryGroup(clip.transform, out var continueBtn, out var sCash, out var sSales, out var sFailed, out var sExp, out var sEarn);

            var nc = clip.AddComponent<NightClipboard>();
            TrySetField(nc, "_config", LoadAsset<NightConfigSO>(NightConfigPath));
            TrySetField(nc, "_grab", grab);
            TrySetField(nc, "_idleGroup", idle);
            TrySetField(nc, "_runningGroup", running);
            TrySetField(nc, "_summaryGroup", summary);
            TrySetField(nc, "_startButton", startBtn);
            TrySetField(nc, "_abortButton", abortBtn);
            TrySetField(nc, "_continueButton", continueBtn);
            TrySetField(nc, "_summaryCash", sCash);
            TrySetField(nc, "_summarySales", sSales);
            TrySetField(nc, "_summaryFailed", sFailed);
            TrySetField(nc, "_summaryExpenses", sExp);
            TrySetField(nc, "_summaryNightlyEarnings", sEarn);
            TrySetField(nc, "_idleNightNumber", idleNight);
            TrySetField(nc, "_idleBestEarnings", idleBest);
            TrySetField(nc, "_idleCash", idleCash);
        }

        private static GameObject BuildIdleGroup(Transform parent, out PokeButton start, out TMP_Text night, out TMP_Text best, out TMP_Text cash)
        {
            var g = new GameObject("IdleGroup");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = new Vector3(0, 0, -0.012f);
            night = MakeLabel(g.transform, "NightNumber", new Vector3(0, 0.15f, 0), "Night 1");
            best = MakeLabel(g.transform, "BestEarnings", new Vector3(0, 0.07f, 0), "Best: $0");
            cash = MakeLabel(g.transform, "Cash", new Vector3(0, 0, 0), "$0");
            start = MakePokeButton(g.transform, "StartButton", new Vector3(0, -0.12f, 0), "START NIGHT");
            return g;
        }

        private static GameObject BuildRunningGroup(Transform parent, out PokeButton abort)
        {
            var g = new GameObject("RunningGroup");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = new Vector3(0, 0, -0.012f);
            abort = MakePokeButton(g.transform, "AbortButton", new Vector3(0, 0, 0), "ABORT");
            g.SetActive(false);
            return g;
        }

        private static GameObject BuildSummaryGroup(Transform parent, out PokeButton cont, out TMP_Text cash, out TMP_Text sales, out TMP_Text failed, out TMP_Text exp, out TMP_Text earn)
        {
            var g = new GameObject("SummaryGroup");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = new Vector3(0, 0, -0.012f);
            cash   = MakeLabel(g.transform, "Cash",     new Vector3(0, 0.16f, 0),  "$0");
            sales  = MakeLabel(g.transform, "Sales",    new Vector3(0, 0.10f, 0),  "Sales: 0");
            failed = MakeLabel(g.transform, "Failed",   new Vector3(0, 0.04f, 0),  "Failed: 0");
            exp    = MakeLabel(g.transform, "Expenses", new Vector3(0, -0.02f, 0), "-$0");
            earn   = MakeLabel(g.transform, "Earnings", new Vector3(0, -0.08f, 0), "+$0");
            cont   = MakePokeButton(g.transform, "ContinueButton", new Vector3(0, -0.16f, 0), "CONTINUE");
            g.SetActive(false);
            return g;
        }

        private static TMP_Text MakeLabel(Transform parent, string name, Vector3 localPos, string text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * 0.008f;
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 8;
            tmp.alignment = TextAlignmentOptions.Center;
            var rt = go.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(20, 4);
            return tmp;
        }

        private static PokeButton MakePokeButton(Transform parent, string name, Vector3 localPos, string label)
        {
            var btn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            btn.name = name;
            btn.transform.SetParent(parent, false);
            btn.transform.localPosition = localPos;
            btn.transform.localScale = new Vector3(0.18f, 0.04f, 0.02f);
            var col = btn.GetComponent<BoxCollider>();
            col.isTrigger = true;
            var poke = btn.AddComponent<PokeButton>();

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(btn.transform, false);
            labelGO.transform.localPosition = new Vector3(0, 0, -0.55f);
            labelGO.transform.localScale = new Vector3(5.5f, 25f, 50f);
            labelGO.transform.localScale = new Vector3(0.006f / 0.18f, 0.006f / 0.04f, 1f);
            var tmp = labelGO.AddComponent<TextMeshPro>();
            tmp.text = label; tmp.fontSize = 4; tmp.alignment = TextAlignmentOptions.Center;
            return poke;
        }

        private static T LoadAsset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        private static void TrySetField(Object target, string field, Object value)
        {
            if (target == null || value == null) return;
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"Field '{field}' not found on {target.GetType().Name}"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void TrySetEnum<TEnum>(Object target, string field, TEnum value) where TEnum : System.Enum
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) return;
            var names = System.Enum.GetNames(typeof(TEnum));
            var cur = value.ToString();
            for (int i = 0; i < names.Length; i++)
                if (names[i] == cur) { p.enumValueIndex = i; break; }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
