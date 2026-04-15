#if UNITY_EDITOR
using System.IO;
using Data.Enums;
using Data.SO;
using UnityEditor;
using UnityEngine;

namespace PourDecisions.EditorTools
{
    /// <summary>
    /// Scaffolds the minimum set of SO assets needed for the MVP under
    /// Assets/Resources/Database/{Ingredients,Bottles,Recipes,Customers,Night}.
    /// Idempotent: existing assets are reused and re-populated with balanced values.
    /// </summary>
    public static class MvpContentBootstrap
    {
        private const string Root = "Assets/Resources/Database";

        [MenuItem("Pour Decisions/Create MVP Content")]
        public static void CreateMvpContent()
        {
            EnsureFolders();

            var vodka = CreateIngredient("Vodka", IngredientId.Vodka, IngredientType.Alcohol, new Color(0.95f, 0.95f, 1f, 0.9f), 30f, 3);
            var gin   = CreateIngredient("Gin",   IngredientId.Gin,   IngredientType.Alcohol, new Color(0.9f,  1f,    0.95f, 0.9f), 30f, 3);
            var tonic = CreateIngredient("Tonic", IngredientId.Tonic, IngredientType.Mixer,   new Color(0.85f, 0.95f, 1f,    0.5f), 40f, 1);
            var cola  = CreateIngredient("Cola",  IngredientId.Cola,  IngredientType.Mixer,   new Color(0.25f, 0.15f, 0.05f, 0.9f), 40f, 1);

            CreateBottle("Bottle_Vodka", vodka, 750f, 50);
            CreateBottle("Bottle_Gin",   gin,   750f, 50);
            CreateBottle("Bottle_Tonic", tonic, 500f, 20);
            CreateBottle("Bottle_Cola",  cola,  500f, 20);

            CreateRecipe("Recipe_VodkaTonic", RecipeId.VodkaTonic, "Vodka Tonic", 8,
                new[] {
                    new RecipeSO.Step { id = IngredientId.Vodka, targetMl = 50f, toleranceMl = 10f },
                    new RecipeSO.Step { id = IngredientId.Tonic, targetMl = 150f, toleranceMl = 20f },
                },
                foreignToleranceMl: 5f);

            CreateRecipe("Recipe_GinTonic", RecipeId.GinTonic, "Gin Tonic", 9,
                new[] {
                    new RecipeSO.Step { id = IngredientId.Gin,   targetMl = 50f, toleranceMl = 10f },
                    new RecipeSO.Step { id = IngredientId.Tonic, targetMl = 150f, toleranceMl = 20f },
                },
                foreignToleranceMl: 5f);

            CreateCustomer("Customer_Regular", "Regular", walkSpeed: 1.4f, patience: 45f, drink: 4f, baseTip: 2);
            CreateCustomer("Customer_Impatient", "Impatient", walkSpeed: 1.6f, patience: 25f, drink: 3f, baseTip: 4);
            CreateCustomer("Customer_Tourist", "Tourist", walkSpeed: 1.2f, patience: 60f, drink: 5f, baseTip: 1);

            CreatePalette("IngredientPalette",
                (IngredientId.Vodka, vodka.LiquidColor),
                (IngredientId.Gin,   gin.LiquidColor),
                (IngredientId.Tonic, tonic.LiquidColor),
                (IngredientId.Cola,  cola.LiquidColor));

            CreateSfxDatabase("SfxDatabase");

            var drunk = CreateDrunkenness("DrunkennessConfig",
                alcoholMlForMax: 60f, maxTip: 1.5f, wobbleAmp: 0.2f, wobbleFreq: 2.0f);

            CreateNightConfig("NightConfig_MVP",
                duration: 180f, spawnInterval: 8f, maxSimultaneous: 3,
                drunkenness: drunk);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MvpContentBootstrap] MVP content created under " + Root);
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", "Database");
            foreach (var sub in new[] { "Ingredients", "Bottles", "Recipes", "Customers", "Night" })
                EnsureFolder(Root, sub);
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static IngredientSO CreateIngredient(string name, IngredientId id, IngredientType type, Color color, float pourRate, int unitCost)
        {
            var path = Root + "/Ingredients/" + name + ".asset";
            var so = LoadOrCreate<IngredientSO>(path);
            var s = new SerializedObject(so);
            s.FindProperty("_id").enumValueIndex = EnumIndex<IngredientId>(id);
            s.FindProperty("_displayName").stringValue = name;
            s.FindProperty("_type").enumValueIndex = EnumIndex<IngredientType>(type);
            s.FindProperty("_liquidColor").colorValue = color;
            s.FindProperty("_pourRateMlPerSec").floatValue = pourRate;
            s.FindProperty("_unitCost").intValue = unitCost;
            s.ApplyModifiedPropertiesWithoutUndo();
            return so;
        }

        private static BottleSO CreateBottle(string name, IngredientSO ingredient, float capacity, int repairCost)
        {
            var path = Root + "/Bottles/" + name + ".asset";
            var so = LoadOrCreate<BottleSO>(path);
            var s = new SerializedObject(so);
            s.FindProperty("_ingredient").objectReferenceValue = ingredient;
            s.FindProperty("_capacityMl").floatValue = capacity;
            s.FindProperty("_repairCost").intValue = repairCost;
            s.ApplyModifiedPropertiesWithoutUndo();
            return so;
        }

        private static RecipeSO CreateRecipe(string assetName, RecipeId id, string displayName, int basePrice, RecipeSO.Step[] steps, float foreignToleranceMl)
        {
            var path = Root + "/Recipes/" + assetName + ".asset";
            var so = LoadOrCreate<RecipeSO>(path);
            var s = new SerializedObject(so);
            s.FindProperty("_id").enumValueIndex = EnumIndex<RecipeId>(id);
            s.FindProperty("_displayName").stringValue = displayName;
            s.FindProperty("_basePrice").intValue = basePrice;
            s.FindProperty("_foreignToleranceMl").floatValue = foreignToleranceMl;

            var stepsProp = s.FindProperty("_steps");
            stepsProp.arraySize = steps.Length;
            for (int i = 0; i < steps.Length; i++)
            {
                var e = stepsProp.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").enumValueIndex = EnumIndex<IngredientId>(steps[i].id);
                e.FindPropertyRelative("targetMl").floatValue = steps[i].targetMl;
                e.FindPropertyRelative("toleranceMl").floatValue = steps[i].toleranceMl;
            }
            s.ApplyModifiedPropertiesWithoutUndo();
            return so;
        }

        private static CustomerSO CreateCustomer(string assetName, string displayName, float walkSpeed, float patience, float drink, int baseTip)
        {
            var path = Root + "/Customers/" + assetName + ".asset";
            var so = LoadOrCreate<CustomerSO>(path);
            var s = new SerializedObject(so);
            s.FindProperty("_displayName").stringValue = displayName;
            s.FindProperty("_walkSpeed").floatValue = walkSpeed;
            s.FindProperty("_patienceSeconds").floatValue = patience;
            s.FindProperty("_drinkSeconds").floatValue = drink;
            s.FindProperty("_baseTip").intValue = baseTip;
            s.ApplyModifiedPropertiesWithoutUndo();
            return so;
        }

        private static IngredientPalette CreatePalette(string assetName, params (IngredientId id, Color color)[] entries)
        {
            var path = Root + "/" + assetName + ".asset";
            var so = LoadOrCreate<IngredientPalette>(path);
            var s = new SerializedObject(so);
            var prop = s.FindProperty("_entries");
            prop.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                var e = prop.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").enumValueIndex = EnumIndex<IngredientId>(entries[i].id);
                e.FindPropertyRelative("color").colorValue = entries[i].color;
            }
            s.ApplyModifiedPropertiesWithoutUndo();
            return so;
        }

        private static SfxDatabase CreateSfxDatabase(string assetName)
        {
            var path = Root + "/" + assetName + ".asset";
            var so = LoadOrCreate<SfxDatabase>(path);
            var s = new SerializedObject(so);
            var prop = s.FindProperty("_entries");

            // Seed the full SfxId enum (minus None) so the asset shows up with
            // placeholder rows in the inspector; user just drops clips on them.
            var ids = new[]
            {
                SfxId.PourLoop, SfxId.GlassBreak, SfxId.BottleBreak,
                SfxId.CashSale, SfxId.CashExpense,
                SfxId.CustomerServed, SfxId.CustomerLeft,
                SfxId.NightStart, SfxId.NightEnd,
                SfxId.ButtonPress,
            };

            if (prop.arraySize < ids.Length) prop.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++)
            {
                var e = prop.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").enumValueIndex = EnumIndex<SfxId>(ids[i]);
                if (e.FindPropertyRelative("volume").floatValue == 0f)
                    e.FindPropertyRelative("volume").floatValue = 0.8f;
                if (e.FindPropertyRelative("pitchMin").floatValue == 0f)
                    e.FindPropertyRelative("pitchMin").floatValue = 0.95f;
                if (e.FindPropertyRelative("pitchMax").floatValue == 0f)
                    e.FindPropertyRelative("pitchMax").floatValue = 1.05f;
                if (e.FindPropertyRelative("spatialBlend").floatValue == 0f)
                    e.FindPropertyRelative("spatialBlend").floatValue = 1f;
                if (ids[i] == SfxId.PourLoop)
                    e.FindPropertyRelative("loop").boolValue = true;
            }
            s.ApplyModifiedPropertiesWithoutUndo();
            return so;
        }

        private static DrunkennessConfigSO CreateDrunkenness(string assetName, float alcoholMlForMax, float maxTip, float wobbleAmp, float wobbleFreq)
        {
            var path = Root + "/Night/" + assetName + ".asset";
            var so = LoadOrCreate<DrunkennessConfigSO>(path);
            var s = new SerializedObject(so);
            s.FindProperty("_alcoholMlForMax").floatValue = alcoholMlForMax;
            s.FindProperty("_maxTipMultiplier").floatValue = maxTip;
            s.FindProperty("_wobbleAmplitude").floatValue = wobbleAmp;
            s.FindProperty("_wobbleFrequency").floatValue = wobbleFreq;
            s.ApplyModifiedPropertiesWithoutUndo();
            return so;
        }

        private static NightConfigSO CreateNightConfig(string assetName, float duration, float spawnInterval, int maxSimultaneous, DrunkennessConfigSO drunkenness)
        {
            var path = Root + "/Night/" + assetName + ".asset";
            var so = LoadOrCreate<NightConfigSO>(path);
            var s = new SerializedObject(so);
            s.FindProperty("_durationSeconds").floatValue = duration;
            s.FindProperty("_spawnIntervalSeconds").floatValue = spawnInterval;
            s.FindProperty("_maxSimultaneous").intValue = maxSimultaneous;
            s.FindProperty("_drunkennessConfig").objectReferenceValue = drunkenness;

            var customers = LoadAllInFolder<CustomerSO>(Root + "/Customers");
            var customerPool = s.FindProperty("_customerPool");
            customerPool.arraySize = customers.Length;
            for (int i = 0; i < customers.Length; i++)
                customerPool.GetArrayElementAtIndex(i).objectReferenceValue = customers[i];

            var recipePool = s.FindProperty("_recipePool");
            var recipes = new[] { RecipeId.VodkaTonic, RecipeId.GinTonic };
            recipePool.arraySize = recipes.Length;
            for (int i = 0; i < recipes.Length; i++)
                recipePool.GetArrayElementAtIndex(i).enumValueIndex = EnumIndex<RecipeId>(recipes[i]);

            s.ApplyModifiedPropertiesWithoutUndo();
            return so;
        }

        private static T[] LoadAllInFolder<T>(string folder) where T : Object
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder });
            var result = new T[guids.Length];
            for (int i = 0; i < guids.Length; i++)
                result[i] = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[i]));
            return result;
        }

        private static int EnumIndex<TEnum>(TEnum value) where TEnum : System.Enum
        {
            var names = System.Enum.GetNames(typeof(TEnum));
            var current = value.ToString();
            for (int i = 0; i < names.Length; i++)
                if (names[i] == current) return i;
            return 0;
        }
    }
}
#endif
