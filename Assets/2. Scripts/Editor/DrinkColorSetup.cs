#if UNITY_EDITOR
using Data.Enums;
using Data.SO;
using Gameplay.Liquid;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EditorTools
{
    /// <summary>
    /// Fase 1 del lenguaje de color por bebida:
    ///  - escribe 5 LiquidColor bien distintos en los IngredientSO de los tragos,
    ///  - sincroniza esas entradas en el IngredientPalette (lo que lee LiquidRenderer),
    ///  - pone LiquidRenderer._colorByFillLevel = false (prefabs Glass + instancias en escena)
    ///    para que el vaso se tiña por BEBIDA y no por nivel.
    /// Idempotente. MCP no puede setear refs/flags de prefab, por eso es un editor tool.
    /// </summary>
    public static class DrinkColorSetup
    {
        private const string IngredientsFolder = "Assets/Resources/Database/Ingredients";
        private const string PalettePath = "Assets/Resources/Database/IngredientPalette.asset";
        private static readonly string[] GlassPrefabs =
        {
            "Assets/4. Prefabs/Glass.prefab",
            "Assets/4. Prefabs/Glass_Asset.prefab",
        };

        private struct Def { public IngredientId id; public string asset; public Color color; }

        // Paleta: 5 hues bien separados (con solo-color prima la distinguibilidad).
        private static readonly Def[] Defs =
        {
            new Def { id = IngredientId.Whiskey,   asset = "Whiskey.asset",   color = new Color(0.910f, 0.510f, 0.180f) }, // naranja
            new Def { id = IngredientId.Cognac,    asset = "Cognac.asset",    color = new Color(0.831f, 0.235f, 0.620f) }, // magenta
            new Def { id = IngredientId.Champagne, asset = "Champagne.asset", color = new Color(0.922f, 0.824f, 0.235f) }, // amarillo
            new Def { id = IngredientId.Wine,      asset = "Wine.asset",      color = new Color(0.478f, 0.200f, 0.710f) }, // púrpura
            new Def { id = IngredientId.Tequila,   asset = "Tequila.asset",   color = new Color(0.184f, 0.710f, 0.478f) }, // verde-teal
        };

        [MenuItem("Pour Decisions/Visuals/Setup Drink Colors")]
        public static void Setup()
        {
            int ing = SetIngredientColors();
            int pal = SyncPalette();
            int glass = SetGlassesTintByIngredient();

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[DrinkColorSetup] {ing} ingredientes, {pal} entradas de palette, {glass} LiquidRenderer -> tinte por bebida.");
        }

        private static int SetIngredientColors()
        {
            int n = 0;
            foreach (var d in Defs)
            {
                var so = AssetDatabase.LoadAssetAtPath<IngredientSO>($"{IngredientsFolder}/{d.asset}");
                if (so == null) { Debug.LogWarning($"[DrinkColorSetup] Falta {d.asset}"); continue; }
                var sob = new SerializedObject(so);
                var p = sob.FindProperty("_liquidColor");
                if (p == null) continue;
                p.colorValue = d.color;
                sob.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(so);
                n++;
            }
            return n;
        }

        private static int SyncPalette()
        {
            var palette = AssetDatabase.LoadAssetAtPath<IngredientPalette>(PalettePath);
            if (palette == null) { Debug.LogWarning("[DrinkColorSetup] Palette no encontrada en " + PalettePath); return 0; }

            var pso = new SerializedObject(palette);
            var arr = pso.FindProperty("_entries");
            if (arr == null) return 0;

            int n = 0;
            foreach (var d in Defs)
            {
                int enumIdx = EnumIndexOf(d.id);
                int found = -1;
                for (int i = 0; i < arr.arraySize; i++)
                {
                    if (arr.GetArrayElementAtIndex(i).FindPropertyRelative("id").enumValueIndex == enumIdx) { found = i; break; }
                }
                if (found < 0)
                {
                    found = arr.arraySize;
                    arr.InsertArrayElementAtIndex(found);
                    arr.GetArrayElementAtIndex(found).FindPropertyRelative("id").enumValueIndex = enumIdx;
                }
                arr.GetArrayElementAtIndex(found).FindPropertyRelative("color").colorValue = d.color;
                n++;
            }
            pso.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(palette);
            return n;
        }

        private static int SetGlassesTintByIngredient()
        {
            int n = 0;

            // Prefabs Glass (edición segura por LoadPrefabContents).
            foreach (var path in GlassPrefabs)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;
                foreach (var lr in root.GetComponentsInChildren<LiquidRenderer>(true))
                    changed |= FlipByFillLevel(lr);
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                if (changed) n++;
            }

            // Instancias en la escena abierta.
            foreach (var lr in Object.FindObjectsByType<LiquidRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (FlipByFillLevel(lr)) n++;

            return n;
        }

        private static bool FlipByFillLevel(LiquidRenderer lr)
        {
            var so = new SerializedObject(lr);
            var p = so.FindProperty("_colorByFillLevel");
            if (p == null || p.boolValue == false) return false;
            p.boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(lr);
            return true;
        }

        private static int EnumIndexOf(IngredientId id)
        {
            var values = (IngredientId[])System.Enum.GetValues(typeof(IngredientId));
            return System.Array.IndexOf(values, id);
        }
    }
}
#endif
