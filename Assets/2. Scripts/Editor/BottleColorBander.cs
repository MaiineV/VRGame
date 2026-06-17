#if UNITY_EDITOR
using Data.SO;
using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    /// <summary>
    /// Fase 3 del lenguaje de color: pone en cada variante de botella un "ColorTag" — un disco
    /// (posavasos) en la base, tinteado al LiquidColor del ingrediente de su BottleSO. Va en la
    /// base (no en el cuerpo) para ser robusto ante las distintas alturas/escalas de cada FBX.
    /// Sin collider (no interfiere con el agarre). Idempotente: re-tintea si ya existe.
    /// </summary>
    public static class BottleColorBander
    {
        private const string BottlesPrefabFolder = "Assets/4. Prefabs/Bottles";
        private const string BottlesSoFolder = "Assets/Resources/Database/Bottles";
        private const string MatFolder = "Assets/4. Prefabs/Bottles/BandMaterials";
        private const string TagName = "ColorTag";

        private static readonly string[] Names =
        { "JackDaniel", "Hennessy", "Champagne", "Wine", "SimpleBottle" };

        [MenuItem("Pour Decisions/Visuals/Build Bottle Color Tags")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(MatFolder))
                AssetDatabase.CreateFolder(BottlesPrefabFolder, "BandMaterials");

            int done = 0;
            foreach (var name in Names)
            {
                var so = AssetDatabase.LoadAssetAtPath<BottleSO>($"{BottlesSoFolder}/Bottle_{name}.asset");
                if (so == null || so.Ingredient == null)
                {
                    Debug.LogWarning($"[BottleColorBander] Bottle_{name}: BottleSO o Ingredient faltante.");
                    continue;
                }
                Color color = so.Ingredient.LiquidColor;
                color.a = 1f;

                var mat = EnsureMaterial(name, color);

                var prefabPath = $"{BottlesPrefabFolder}/Bottle_{name}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                {
                    Debug.LogWarning($"[BottleColorBander] Falta prefab {prefabPath}");
                    continue;
                }

                var root = PrefabUtility.LoadPrefabContents(prefabPath);
                var tag = root.transform.Find(TagName);
                GameObject tagGo;
                if (tag != null)
                {
                    tagGo = tag.gameObject;
                }
                else
                {
                    tagGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    tagGo.name = TagName;
                    tagGo.transform.SetParent(root.transform, false);
                    var col = tagGo.GetComponent<Collider>();
                    if (col != null) Object.DestroyImmediate(col);
                }
                // Flat disc at the base; ~0.13 m wide, ~0.02 m tall.
                tagGo.transform.localPosition = new Vector3(0f, 0.012f, 0f);
                tagGo.transform.localRotation = Quaternion.identity;
                tagGo.transform.localScale = new Vector3(0.13f, 0.012f, 0.13f);

                var rend = tagGo.GetComponent<MeshRenderer>();
                if (rend != null) rend.sharedMaterial = mat;

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                PrefabUtility.UnloadPrefabContents(root);
                done++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[BottleColorBander] Color tags en {done} botellas.");
        }

        private static Material EnsureMaterial(string name, Color color)
        {
            var path = $"{MatFolder}/Band_{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit")
                          ?? Shader.Find("Universal Render Pipeline/Lit")
                          ?? Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            // Cover both URP (_BaseColor) and built-in (_Color) property names.
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mat.color = color;
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
#endif
