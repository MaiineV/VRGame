#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Data.Enums;
using Data.SO;
using Gameplay.Interactions;
using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    /// <summary>
    /// Builds one Prefab Variant of <c>Bottle.prefab</c> per real bottle model, so swapping the
    /// 3D model of a shelf bottle is a drag-and-drop instead of a hand mesh edit. For each model it
    /// also ensures the backing IngredientSO + BottleSO exist (content/economy), auto-fits the
    /// CapsuleCollider and the Neck (pour origin) to the model, and normalizes the model height.
    /// Idempotent: re-running re-creates the variants and leaves existing assets in place.
    /// </summary>
    public static class BottleVariantBuilder
    {
        private const string BasePrefabPath = "Assets/4. Prefabs/Bottle.prefab";
        private const string IngredientsDir = "Assets/Resources/Database/Ingredients";
        private const string BottlesDir = "Assets/Resources/Database/Bottles";
        private const string RecipesDir = "Assets/Resources/Database/Recipes";
        private const string VariantsDir = "Assets/4. Prefabs/Bottles";

        private const float TargetHeight = 0.3f;   // meters; matches the base collider/neck scale
        private const float CapacityMl = 1000000f;  // 1000 L — intentionally huge so bottles never run dry while testing
        private const int RepairCost = 50;

        private struct Def
        {
            public string Name;          // variant + bottle asset suffix
            public string FbxPath;
            public IngredientId Id;
            public string IngredientName; // ingredient asset + display name
            public string BottleDisplay;
            public Color Liquid;
            public float PourRate;
            public int UnitCost;
            public RecipeId Recipe;       // drink customers can request
            public int BasePrice;
        }

        private static readonly Def[] Defs =
        {
            new Def { Name = "JackDaniel",   FbxPath = "Assets/7.Models/JackDaniel.fbx",
                      Id = IngredientId.Whiskey, IngredientName = "Whiskey", BottleDisplay = "Jack Daniel's",
                      Liquid = new Color(0.55f, 0.30f, 0.05f), PourRate = 30f, UnitCost = 2,
                      Recipe = RecipeId.Whiskey, BasePrice = 8 },
            new Def { Name = "Hennessy",     FbxPath = "Assets/7.Models/Hennessy.fbx",
                      Id = IngredientId.Cognac, IngredientName = "Cognac", BottleDisplay = "Hennessy",
                      Liquid = new Color(0.45f, 0.22f, 0.05f), PourRate = 28f, UnitCost = 3,
                      Recipe = RecipeId.Cognac, BasePrice = 10 },
            new Def { Name = "Champagne",    FbxPath = "Assets/7.Models/Champagne.fbx",
                      Id = IngredientId.Champagne, IngredientName = "Champagne", BottleDisplay = "Champagne",
                      Liquid = new Color(0.92f, 0.85f, 0.55f), PourRate = 35f, UnitCost = 3,
                      Recipe = RecipeId.Champagne, BasePrice = 10 },
            new Def { Name = "Wine",         FbxPath = "Assets/7.Models/Wine.fbx",
                      Id = IngredientId.Wine, IngredientName = "Wine", BottleDisplay = "Red Wine",
                      Liquid = new Color(0.45f, 0.05f, 0.12f), PourRate = 32f, UnitCost = 2,
                      Recipe = RecipeId.Wine, BasePrice = 7 },
            new Def { Name = "SimpleBottle", FbxPath = "Assets/7.Models/low-poly-bottle/source/simple bottle.fbx",
                      Id = IngredientId.Tequila, IngredientName = "Tequila", BottleDisplay = "Tequila",
                      Liquid = new Color(0.85f, 0.80f, 0.55f), PourRate = 33f, UnitCost = 2,
                      Recipe = RecipeId.Tequila, BasePrice = 7 },
        };

        [MenuItem("Pour Decisions/Visuals/Build Bottle Variants")]
        public static void Build()
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
            if (basePrefab == null) { Debug.LogError($"[BottleVariantBuilder] Base prefab not found at {BasePrefabPath}."); return; }

            EnsureDir(IngredientsDir);
            EnsureDir(BottlesDir);
            EnsureDir(RecipesDir);
            EnsureDir(VariantsDir);

            int built = 0;
            var recipeIds = new List<RecipeId>();
            foreach (var def in Defs)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(def.FbxPath);
                if (model == null) { Debug.LogError($"[BottleVariantBuilder] Model not found: {def.FbxPath} (skipping {def.Name})."); continue; }

                var ingredient = EnsureIngredient(def);
                var bottleSO = EnsureBottle(def, ingredient);
                EnsureRecipe(def, ingredient);
                recipeIds.Add(def.Recipe);

                var variant = BuildVariant(def, basePrefab, model, bottleSO);
                if (variant == null) continue;

                // Close the loop: the SO references its visual variant.
                SetField(bottleSO, "_prefab", variant);
                built++;
            }

            int pools = AddRecipesToNightPools(recipeIds);
            int bumped = BumpAllBottleCapacities();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BottleVariantBuilder] Built {built}/{Defs.Length} bottle variants into {VariantsDir}; " +
                      $"added {recipeIds.Count} drinks to {pools} night recipe pool(s); " +
                      $"set {bumped} bottle(s) to {CapacityMl} ml for testing.");
        }

        /// <summary>Sets every BottleSO to the huge testing capacity so nothing runs dry during playtests.</summary>
        private static int BumpAllBottleCapacities()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:BottleSO"))
            {
                var cfg = AssetDatabase.LoadAssetAtPath<BottleSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (cfg == null) continue;
                SetField(cfg, "_capacityMl", CapacityMl);
                n++;
            }
            return n;
        }

        private static RecipeSO EnsureRecipe(Def def, IngredientSO ingredient)
        {
            string path = $"{RecipesDir}/Recipe_{def.IngredientName}.asset";
            var so = AssetDatabase.LoadAssetAtPath<RecipeSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<RecipeSO>();
                AssetDatabase.CreateAsset(so, path);
            }
            SetField(so, "_id", (int)def.Recipe);
            SetField(so, "_displayName", def.BottleDisplay);
            SetField(so, "_basePrice", def.BasePrice);
            EditorUtility.SetDirty(so);
            return so;
        }

        /// <summary>
        /// Appends the new drinks to the _recipePool of every NightConfigSO in the project so NPCs
        /// can request them. Idempotent — existing entries are not duplicated.
        /// </summary>
        private static int AddRecipesToNightPools(List<RecipeId> recipeIds)
        {
            int touched = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:NightConfigSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var cfg = AssetDatabase.LoadAssetAtPath<NightConfigSO>(path);
                if (cfg == null) continue;

                var so = new SerializedObject(cfg);
                var pool = so.FindProperty("_recipePool");
                if (pool == null || !pool.isArray) continue;

                var existing = new HashSet<int>();
                for (int i = 0; i < pool.arraySize; i++)
                    existing.Add(pool.GetArrayElementAtIndex(i).intValue);

                bool changed = false;
                foreach (var id in recipeIds)
                {
                    if (existing.Contains((int)id)) continue;
                    pool.arraySize++;
                    pool.GetArrayElementAtIndex(pool.arraySize - 1).intValue = (int)id;
                    existing.Add((int)id);
                    changed = true;
                }
                if (changed)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(cfg);
                    touched++;
                }
            }
            return touched;
        }

        private static IngredientSO EnsureIngredient(Def def)
        {
            string path = $"{IngredientsDir}/{def.IngredientName}.asset";
            var so = AssetDatabase.LoadAssetAtPath<IngredientSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<IngredientSO>();
                AssetDatabase.CreateAsset(so, path);
            }
            SetField(so, "_id", (int)def.Id);
            SetField(so, "_displayName", def.IngredientName);
            SetField(so, "_type", (int)IngredientType.Alcohol);
            SetField(so, "_liquidColor", def.Liquid);
            SetField(so, "_pourRateMlPerSec", def.PourRate);
            SetField(so, "_unitCost", def.UnitCost);
            EditorUtility.SetDirty(so);
            return so;
        }

        private static BottleSO EnsureBottle(Def def, IngredientSO ingredient)
        {
            string path = $"{BottlesDir}/Bottle_{def.Name}.asset";
            var so = AssetDatabase.LoadAssetAtPath<BottleSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<BottleSO>();
                AssetDatabase.CreateAsset(so, path);
            }
            SetField(so, "_ingredient", ingredient);
            SetField(so, "_capacityMl", CapacityMl);
            SetField(so, "_repairCost", RepairCost);
            EditorUtility.SetDirty(so);
            return so;
        }

        private static GameObject BuildVariant(Def def, GameObject basePrefab, GameObject model, BottleSO bottleSO)
        {
            // Connected instance of the base -> SaveAsPrefabAsset turns it into a Variant.
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            try
            {
                // Hide the base placeholder mesh.
                var body = instance.transform.Find("Body");
                if (body != null) body.gameObject.SetActive(false);

                // Drop the model in as the visual.
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
                visual.name = "Visual";
                visual.transform.SetParent(instance.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;

                if (TryGetLocalBounds(visual, instance.transform, out var b))
                {
                    float scale = b.size.y > 1e-4f ? TargetHeight / b.size.y : 1f;
                    visual.transform.localScale = Vector3.one * scale;

                    // Bounds scale linearly about the root origin (visual sits at 0).
                    Vector3 min = b.min * scale, max = b.max * scale;
                    float h = max.y - min.y;
                    // Sit the base on y=0 and center it on the bottle axis.
                    visual.transform.localPosition = new Vector3(-(min.x + max.x) * 0.5f, -min.y, -(min.z + max.z) * 0.5f);

                    FitColliderAndNeck(instance, h, Mathf.Max(max.x - min.x, max.z - min.z) * 0.5f);
                }
                else
                {
                    Debug.LogWarning($"[BottleVariantBuilder] {def.Name}: model has no renderers; left collider/neck at base defaults.");
                }

                // Wire content.
                var bottle = instance.GetComponent<Bottle>();
                if (bottle != null) SetField(bottle, "_so", bottleSO);

                string path = $"{VariantsDir}/Bottle_{def.Name}.prefab";
                AssetDatabase.DeleteAsset(path); // rebuild cleanly if it exists
                var variant = PrefabUtility.SaveAsPrefabAsset(instance, path, out bool ok);
                if (!ok) { Debug.LogError($"[BottleVariantBuilder] Failed to save variant {path}."); return null; }
                return variant;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void FitColliderAndNeck(GameObject instance, float height, float radius)
        {
            var capsule = instance.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                capsule.direction = 1; // Y
                capsule.height = Mathf.Max(height, 0.01f);
                capsule.radius = Mathf.Clamp(radius, 0.01f, height * 0.5f);
                capsule.center = new Vector3(0f, height * 0.5f, 0f);
            }

            var neck = instance.transform.Find("Neck");
            if (neck != null) neck.localPosition = new Vector3(0f, height, 0f);
        }

        /// <summary>Combined renderer bounds expressed in <paramref name="space"/>'s local frame.</summary>
        private static bool TryGetLocalBounds(GameObject go, Transform space, out Bounds bounds)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            bounds = default;
            bool any = false;
            foreach (var r in renderers)
            {
                if (r is ParticleSystemRenderer) continue;
                var wb = r.bounds; // world
                // Convert the 8 corners to local space so the result is correct under rotation.
                for (int i = 0; i < 8; i++)
                {
                    Vector3 c = wb.center + Vector3.Scale(wb.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 local = space.InverseTransformPoint(c);
                    if (!any) { bounds = new Bounds(local, Vector3.zero); any = true; }
                    else bounds.Encapsulate(local);
                }
            }
            return any;
        }

        private static void EnsureDir(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }
        }

        private static void SetField(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogWarning($"[BottleVariantBuilder] Field '{field}' not found on {target.name}."); return; }

            switch (value)
            {
                case int i: prop.intValue = i; break;
                case float f: prop.floatValue = f; break;
                case string s: prop.stringValue = s; break;
                case Color c: prop.colorValue = c; break;
                case Object o: prop.objectReferenceValue = o; break;
                default: Debug.LogWarning($"[BottleVariantBuilder] Unsupported value type for '{field}'."); break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
#endif
