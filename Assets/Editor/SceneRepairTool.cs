using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace VRGame.EditorTools
{
    /// <summary>
    /// One-shot repair utility. The MCP bridge cannot assign UnityEngine.Object
    /// references (Transforms, meshes, materials), so this script does it from
    /// inside the editor where AssetDatabase / SerializedObject are available.
    /// Safe to delete after running.
    /// </summary>
    public static class SceneRepairTool
    {
        const string BottlePrefabPath = "Assets/4. Prefabs/Bottle.prefab";
        const string StreamMatPath = "Assets/4. Prefabs/PourStreamMat.mat";
        const string Bottle5SoPath = "Assets/Resources/Database/Bottles/Bottle_Vodka.asset";

        [MenuItem("Tools/Repair/Create Pour Streams")]
        public static void CreatePourStreams()
        {
            var mat = GetOrCreateStreamMaterial();

            // --- 1) Bottle.prefab -> all 6 shelf bottles inherit the stream ---
            var prefab = PrefabUtility.LoadPrefabContents(BottlePrefabPath);
            try
            {
                AddStreamTo(prefab, mat, out _);
                PrefabUtility.SaveAsPrefabAsset(prefab, BottlePrefabPath);
                Debug.Log("[SceneRepairTool] PourStream added to Bottle.prefab (6 shelf bottles).");
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }

            // --- 2) Bottle5 (scene object) ---
            var bottle5 = GameObject.Find("Bottle5");
            if (bottle5 != null)
            {
                AddStreamTo(bottle5, mat, out _);

                // Give Bottle5 a BottleSO so it actually pours (it had none).
                var so = AssetDatabase.LoadAssetAtPath<Data.SO.BottleSO>(Bottle5SoPath);
                var bottle = bottle5.GetComponent<Gameplay.Interactions.Bottle>();
                if (so != null && bottle != null)
                {
                    var sob = new SerializedObject(bottle);
                    sob.FindProperty("_so").objectReferenceValue = so;
                    sob.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(bottle);
                    Debug.Log($"[SceneRepairTool] Bottle5._so set to '{so.name}'.");
                }
                else
                {
                    Debug.LogWarning("[SceneRepairTool] Could not assign BottleSO to Bottle5 (asset or component missing).");
                }

                EditorSceneManager.MarkSceneDirty(bottle5.scene);
                EditorSceneManager.SaveScene(bottle5.scene);
                Debug.Log("[SceneRepairTool] PourStream added to Bottle5 and scene saved.");
            }
            else
            {
                Debug.LogWarning("[SceneRepairTool] 'Bottle5' not found in the open scene.");
            }
        }

        /// <summary>
        /// Builds the PourStream hierarchy under 'bottleRoot' and wires it into the
        /// bottle's PourDetector._stream. Geometry runs along the StreamScaler local +Z,
        /// pivoted at the origin (the neck), which — combined with PourDetector/PourStream's
        /// rotation math — makes it point from the neck to the container.
        /// </summary>
        static void AddStreamTo(GameObject bottleRoot, Material mat, out Gameplay.Liquid.PourStream stream)
        {
            stream = null;
            var pd = bottleRoot.GetComponent<Gameplay.Liquid.PourDetector>();
            if (pd == null) { Debug.LogWarning($"[SceneRepairTool] No PourDetector on '{bottleRoot.name}'."); return; }

            // Re-run safety: remove a previous PourStream child.
            var prev = bottleRoot.transform.Find("PourStream");
            if (prev != null) Object.DestroyImmediate(prev.gameObject);

            // Root (carries PourStream.cs). Show() drives its world position/rotation.
            var root = new GameObject("PourStream");
            root.transform.SetParent(bottleRoot.transform, false);
            var ps = root.AddComponent<Gameplay.Liquid.PourStream>();

            // Scaler = the transform PourStream scales to (radius, radius, length) on Z.
            // localRotation (90,0,0) maps its +Z onto the root's -Y (down), which Show()
            // then rotates onto the pour direction.
            var scaler = new GameObject("StreamScaler");
            scaler.transform.SetParent(root.transform, false);
            scaler.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            scaler.transform.localScale = Vector3.one;

            // The visible cylinder: oriented along the scaler's +Z, spanning [0,1] from the origin.
            var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyl.name = "StreamCylinder";
            var col = cyl.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            cyl.transform.SetParent(scaler.transform, false);
            cyl.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // cylinder +Y -> scaler +Z
            cyl.transform.localScale = new Vector3(1f, 0.5f, 1f);         // default height 2 -> length 1
            cyl.transform.localPosition = new Vector3(0f, 0f, 0.5f);      // pivot at origin, grow +Z
            var mr = cyl.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;

            // Wire PourStream._mesh and ._renderer.
            var psSo = new SerializedObject(ps);
            psSo.FindProperty("_mesh").objectReferenceValue = scaler.transform;
            psSo.FindProperty("_renderer").objectReferenceValue = mr;
            psSo.ApplyModifiedPropertiesWithoutUndo();

            // Wire PourDetector._stream.
            var pdSo = new SerializedObject(pd);
            pdSo.FindProperty("_stream").objectReferenceValue = ps;
            pdSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pd);

            stream = ps;
        }

        static Material GetOrCreateStreamMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(StreamMatPath);
            if (mat != null) return mat;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader) { name = "PourStreamMat" };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.8f);
            AssetDatabase.CreateAsset(mat, StreamMatPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SceneRepairTool] Created material at {StreamMatPath} (shader: {shader.name}).");
            return mat;
        }
    }
}
