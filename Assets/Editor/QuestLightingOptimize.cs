using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.SceneManagement;

namespace VRGame.EditorTools
{
    /// <summary>
    /// One-shot Quest 2 lighting optimisation utilities.
    /// Run each menu item independently under Tools/Optimize/.
    ///
    /// Workflow (run in order for a full bake pipeline):
    ///   1. Tools/Optimize/Configure Shadows For Quest
    ///   2. Tools/Optimize/Mark Static Geometry
    ///   3. Tools/Optimize/Set Directional Light Mixed
    ///   4. Tools/Optimize/Bake Lightmaps (Quest)
    /// </summary>
    public static class QuestLightingOptimize
    {
        // ---------------------------------------------------------------
        //  Paths
        // ---------------------------------------------------------------
        const string URPAssetPath = "Assets/Settings/Mobile_RPAsset.asset";

        // ---------------------------------------------------------------
        //  Name fragments that identify DYNAMIC objects (case-insensitive).
        //  Objects whose names contain any of these are skipped during
        //  static-flag assignment.
        // ---------------------------------------------------------------
        static readonly string[] DynamicNameFragments = new[]
        {
            "bottle", "glass", "cup", "customer", "player",
            "hand", "controller", "rig", "camera", "ui", "canvas",
            // Moving / runtime-updated meshes that must NOT be static-batched:
            "stream", "cylinder", "pour", "clipboard", "marker", "button"
        };

        // Name fragments that, if found on ANY ancestor, disqualify the object
        // (e.g. "StreamCylinder" lives under "PourStream"/"StreamScaler").
        static readonly string[] DynamicAncestorFragments = new[]
        {
            "pour", "stream", "bottle", "clipboard"
        };

        // ---------------------------------------------------------------
        //  Component types that indicate a dynamic object.
        //  We check by type name (string) so we don't need hard assembly
        //  references to OVR/interaction packages.
        // ---------------------------------------------------------------
        static readonly string[] DynamicComponentTypeNames = new[]
        {
            "Rigidbody",
            "CustomerEntity",
            // Common OVR / XRI grab component names:
            "OVRGrabbable",
            "XRGrabInteractable",
            "XRBaseInteractable",
            "Grabbable",
            // Runtime-updated meshes / interactive gameplay objects:
            "PourStream",
            "Bottle",
            "Glass",
            "PokeButton",
            // TextMeshPro labels rebuild their mesh when text changes — never batch:
            "TextMeshPro",
            "TextMeshProUGUI",
            "TMP_Text",
        };

        // ---------------------------------------------------------------
        //  1. Configure Shadows For Quest
        // ---------------------------------------------------------------

        [MenuItem("Tools/Optimize/Configure Shadows For Quest")]
        public static void ConfigureShadowsForQuest()
        {
            var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(URPAssetPath);
            if (urpAsset == null)
            {
                Debug.LogWarning($"[QuestLightingOptimize] URP asset not found at '{URPAssetPath}'. Aborting.");
                return;
            }

            // Use SerializedObject for fields that have no public setter in URP 17.
            var so = new SerializedObject(urpAsset);

            // --- Shadow distance: 15 m (down from 50 m) ---
            // A small bar interior is <10 m across; 15 m covers everything
            // with headroom. Shadow pass cost scales with the view frustum
            // area covered, so this is a large win.
            SetFloat(so, "m_ShadowDistance", 15f, "shadow distance");

            // --- Cascade count: 1 ---
            // Multiple cascades make sense for open-world outdoor scenes.
            // For a single small interior room a single cascade is optimal.
            // (The asset already has m_ShadowCascadeCount: 1 but we confirm
            // and log to make the operation idempotent.)
            SetInt(so, "m_ShadowCascadeCount", 1, "shadow cascade count");

            // --- Main-light shadowmap resolution: 512 ---
            // At 15 m distance a 512-texel shadowmap gives ~3 cm/texel on
            // the floor — imperceptibly coarse on Quest 2's 1832x1920 per-eye
            // panel. Dropping from 1024 saves 75% shadowmap memory and roughly
            // the same fraction of shadow-pass fragment cost.
            // LightmapResolution enum: 256=256, 512=512, 1024=1024, 2048=2048.
            SetInt(so, "m_MainLightShadowmapResolution", 512, "main-light shadowmap resolution");

            // --- MSAA: keep at 4 ---
            // Quest 2 uses a tiled GPU that resolves MSAA on-chip at near-zero
            // bandwidth cost. Disabling it would only re-introduce aliasing
            // while saving virtually nothing on tile-based hardware.
            var msaa = so.FindProperty("m_MSAA");
            if (msaa != null)
                Debug.Log($"[QuestLightingOptimize] MSAA kept at {msaa.intValue}x (correct for Quest tiled GPU — no change).");
            else
                Debug.LogWarning("[QuestLightingOptimize] Could not find m_MSAA property — leaving untouched.");

            // --- HDR: keep off ---
            var hdr = so.FindProperty("m_SupportsHDR");
            if (hdr != null)
                Debug.Log($"[QuestLightingOptimize] HDR is {(hdr.boolValue ? "ON" : "OFF")} — keeping off (correct for Quest).");
            else
                Debug.LogWarning("[QuestLightingOptimize] Could not find m_SupportsHDR property — leaving untouched.");

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(urpAsset);
            AssetDatabase.SaveAssets();

            Debug.Log("[QuestLightingOptimize] Configure Shadows For Quest: DONE. " +
                      "Shadow distance=15 m, cascades=1, shadowmap=512, MSAA=4, HDR=off.");
        }

        // ---------------------------------------------------------------
        //  2. Mark Static Geometry
        // ---------------------------------------------------------------

        [MenuItem("Tools/Optimize/Mark Static Geometry")]
        public static void MarkStaticGeometry()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogWarning("[QuestLightingOptimize] No valid active scene open. Aborting.");
                return;
            }

            int flagged = 0;
            int skipped = 0;

            foreach (var root in scene.GetRootGameObjects())
                ProcessGameObjectRecursive(root, ref flagged, ref skipped);

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[QuestLightingOptimize] Mark Static Geometry: DONE. " +
                      $"Flagged={flagged}, Skipped={skipped}. Save the scene to persist.");
        }

        static void ProcessGameObjectRecursive(GameObject go, ref int flagged, ref int skipped)
        {
            if (ShouldMarkStatic(go))
            {
                // ContributeGI enables baked lightmap reception/contribution.
                // BatchingStatic enables static batching to cut draw calls.
                const StaticEditorFlags flags =
                    StaticEditorFlags.ContributeGI |
                    StaticEditorFlags.BatchingStatic |
                    StaticEditorFlags.OccludeeStatic |
                    StaticEditorFlags.OccluderStatic |
                    StaticEditorFlags.ReflectionProbeStatic;

                GameObjectUtility.SetStaticEditorFlags(go, flags);
                EditorUtility.SetDirty(go);
                Debug.Log($"[QuestLightingOptimize]   FLAGGED: '{go.name}'");
                flagged++;
            }
            else
            {
                Debug.Log($"[QuestLightingOptimize]   SKIPPED: '{go.name}'");
                skipped++;
            }

            foreach (Transform child in go.transform)
                ProcessGameObjectRecursive(child.gameObject, ref flagged, ref skipped);
        }

        static bool ShouldMarkStatic(GameObject go)
        {
            // Must have both a MeshRenderer and MeshFilter to be renderable static geometry.
            if (go.GetComponent<MeshRenderer>() == null) return false;
            if (go.GetComponent<MeshFilter>()   == null) return false;

            // Skip ALL UI — world-space TextMeshPro labels and buttons live under a
            // Canvas and rebuild their mesh at runtime; static-batching freezes them.
            if (go.GetComponent<RectTransform>() != null) return false;
            if (go.GetComponentInParent<Canvas>(true) != null) return false;

            // Skip if any ANCESTOR name marks a dynamic hierarchy (e.g. StreamCylinder
            // under PourStream, a bottle's child meshes, etc.).
            for (var t = go.transform.parent; t != null; t = t.parent)
            {
                string parentLower = t.name.ToLowerInvariant();
                foreach (var frag in DynamicAncestorFragments)
                    if (parentLower.Contains(frag))
                        return false;
            }

            // Skip objects with dynamic component types (checked by type name to
            // avoid hard references to OVR / XRI assemblies).
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue; // missing script guard
                string typeName = comp.GetType().Name;
                foreach (var dynType in DynamicComponentTypeNames)
                    if (string.Equals(typeName, dynType, StringComparison.OrdinalIgnoreCase))
                        return false;
            }

            // Skip objects whose names suggest dynamic / interactive content.
            string nameLower = go.name.ToLowerInvariant();
            foreach (var fragment in DynamicNameFragments)
                if (nameLower.Contains(fragment))
                    return false;

            return true;
        }

        // ---------------------------------------------------------------
        //  3. Set Directional Light Mixed
        // ---------------------------------------------------------------

        [MenuItem("Tools/Optimize/Set Directional Light Mixed")]
        public static void SetDirectionalLightMixed()
        {
            // Find the first enabled Directional light in the scene.
            Light dirLight = null;
            foreach (var light in GameObject.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional && light.enabled)
                {
                    dirLight = light;
                    break;
                }
            }

            if (dirLight == null)
            {
                Debug.LogWarning("[QuestLightingOptimize] No enabled Directional light found in the active scene. Aborting.");
                return;
            }

            // LightmapBakeType.Mixed: realtime direct + baked indirect.
            // Dynamic objects (NPCs, player hands, bottles) still receive
            // the realtime direct shadow from the sun/ambient key light,
            // while static surfaces bake indirect/bounce GI — giving the
            // best visual quality at the lowest runtime cost for this setup.
            var so = new SerializedObject(dirLight);
            var lightmappingProp = so.FindProperty("m_Lightmapping");
            if (lightmappingProp == null)
            {
                // Fallback: use the public API (available in most Unity versions).
                dirLight.lightmapBakeType = LightmapBakeType.Mixed;
                EditorUtility.SetDirty(dirLight);
                Debug.Log($"[QuestLightingOptimize] '{dirLight.name}' set to Mixed via public API.");
            }
            else
            {
                // m_Lightmapping values: 4=Realtime, 1=Mixed, 2=Baked
                lightmappingProp.intValue = 1;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(dirLight);
                Debug.Log($"[QuestLightingOptimize] '{dirLight.name}' m_Lightmapping set to 1 (Mixed) via SerializedObject.");
            }

            EditorSceneManager.MarkSceneDirty(dirLight.gameObject.scene);
            Debug.Log("[QuestLightingOptimize] Set Directional Light Mixed: DONE. " +
                      "Dynamic objects (NPCs/hands/bottles) keep realtime direct; statics bake indirect.");
        }

        // ---------------------------------------------------------------
        //  4. Bake Lightmaps (Quest)
        // ---------------------------------------------------------------

        [MenuItem("Tools/Optimize/Bake Lightmaps (Quest)")]
        public static void BakeLightmapsQuest()
        {
            // --- Apply Quest-appropriate lightmap settings ---
            // We write through SerializedObject on the scene's LightmapSettings
            // (fileID 157 &3 in Bar.unity) using Lightmapping.lightingSettings
            // if a LightingSettings asset exists, otherwise fall back to the
            // legacy SerializedObject path on LightmapEditorSettings.

            // NOTE: Lightmapping.lightingSettings THROWS (does not return null) when no
            // LightingSettings asset is assigned. So we never read the getter blindly:
            // load our asset from disk, create it if missing, then assign it.
            const string lsPath = "Assets/Settings/QuestLightingSettings.asset";
            var lightingSettings = AssetDatabase.LoadAssetAtPath<LightingSettings>(lsPath);
            if (lightingSettings == null)
            {
                Debug.Log($"[QuestLightingOptimize] Creating LightingSettings at {lsPath}.");
                lightingSettings = new LightingSettings { name = "QuestLightingSettings" };
                AssetDatabase.CreateAsset(lightingSettings, lsPath);
                AssetDatabase.SaveAssets();
            }
            Lightmapping.lightingSettings = lightingSettings;
            ApplyLightingSettingsAsset(lightingSettings);

            // --- Kick off the bake ---
            if (Lightmapping.isRunning)
            {
                Debug.LogWarning("[QuestLightingOptimize] A lightmap bake is already in progress. Cancel it first.");
                return;
            }

            Debug.Log("[QuestLightingOptimize] Bake Lightmaps (Quest): STARTING async bake. " +
                      "Resolution=30 tx/unit, atlas=1024, mode=NonDirectional, compressed=true. " +
                      "Monitor the Lighting window for progress.");

            Lightmapping.BakeAsync();
        }

        static void ApplyLightingSettingsAsset(LightingSettings settings)
        {
            var so = new SerializedObject(settings);

            // Bake resolution: 30 texels/unit.
            // The scene currently uses 40; 30 is a conservative step down that
            // keeps lightmap quality acceptable while reducing atlas count and
            // bake time. Range recommended for mobile: 20-40.
            SetFloat(so, "m_BakeResolution", 30f, "bake resolution (texels/unit)");

            // Max atlas size: 1024.
            // Keeps per-lightmap GPU memory bounded. A small bar interior will
            // rarely need more than one atlas at 1024.
            SetInt(so, "m_AtlasSize", 1024, "atlas size");

            // Lightmap mode: NonDirectional (value 1).
            // Directional mode stores a second texture per lightmap that encodes
            // dominant light direction — useful for normal-map response but costs
            // 50% extra lightmap memory and bandwidth. NonDirectional is the
            // standard recommendation for Quest 2 mobile targets.
            // LightmapsMode: NonDirectional=1 (was already 1 in the scene YAML).
            SetInt(so, "m_LightmapsBakeMode", 1, "lightmap mode (NonDirectional=1)");

            // Texture compression: on.
            // ETC2 on Android/Vulkan. No perceptible quality difference at
            // Quest 2 display density for lightmaps.
            SetBool(so, "m_TextureCompression", true, "texture compression");

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            Debug.Log("[QuestLightingOptimize] LightingSettings applied: " +
                      "resolution=30, atlas=1024, NonDirectional, compressed=true.");
        }

        // ---------------------------------------------------------------
        //  SerializedObject helpers  (mirror style from SceneRepairTool.cs)
        // ---------------------------------------------------------------

        static void SetFloat(SerializedObject so, string propName, float value, string label)
        {
            var prop = so.FindProperty(propName);
            if (prop == null)
            {
                Debug.LogWarning($"[QuestLightingOptimize] Property '{propName}' not found on {so.targetObject?.GetType().Name}. Skipping {label}.");
                return;
            }
            float old = prop.floatValue;
            prop.floatValue = value;
            Debug.Log($"[QuestLightingOptimize]   {label}: {old} -> {value}");
        }

        static void SetInt(SerializedObject so, string propName, int value, string label)
        {
            var prop = so.FindProperty(propName);
            if (prop == null)
            {
                Debug.LogWarning($"[QuestLightingOptimize] Property '{propName}' not found on {so.targetObject?.GetType().Name}. Skipping {label}.");
                return;
            }
            int old = prop.intValue;
            prop.intValue = value;
            Debug.Log($"[QuestLightingOptimize]   {label}: {old} -> {value}");
        }

        static void SetBool(SerializedObject so, string propName, bool value, string label)
        {
            var prop = so.FindProperty(propName);
            if (prop == null)
            {
                Debug.LogWarning($"[QuestLightingOptimize] Property '{propName}' not found on {so.targetObject?.GetType().Name}. Skipping {label}.");
                return;
            }
            bool old = prop.boolValue;
            prop.boolValue = value;
            Debug.Log($"[QuestLightingOptimize]   {label}: {old} -> {value}");
        }
    }
}
