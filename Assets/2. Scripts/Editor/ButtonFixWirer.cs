#if UNITY_EDITOR
using Data.SO;
using Gameplay;
using Gameplay.Interactions;
using UI.Diegetic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EditorTools
{
    /// <summary>
    /// One-shot fixer for the two reported button bugs:
    ///  1) Glass spawn poke never fired because its PokeButton only listened to the "Hand" layer (7),
    ///     while the PokeFinger colliders live on layer 0 — so the finger never matched. We widen the
    ///     spawn button to all layers, matching the other (working) poke buttons.
    ///  2) The A button could silently fail to start the night when no clipboard staged a NightConfig.
    ///     We assign GameControls._fallbackConfig from the clipboard's config so A always works.
    /// </summary>
    public static class ButtonFixWirer
    {
        [MenuItem("Pour Decisions/Fix Night and Glass Buttons")]
        public static void Fix()
        {
            FixGlassSpawnButtonLayers();
            WireGameControlsFallbackConfig();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[ButtonFixWirer] Done.");
        }

        private static void FixGlassSpawnButtonLayers()
        {
            var dispenser = Object.FindFirstObjectByType<GlassDispenser>();
            if (dispenser == null) { Debug.LogError("[ButtonFixWirer] No GlassDispenser in scene."); return; }

            var so = new SerializedObject(dispenser);
            var btnRef = so.FindProperty("_spawnButton").objectReferenceValue as PokeButton;
            if (btnRef == null) { Debug.LogError("[ButtonFixWirer] GlassDispenser._spawnButton not assigned."); return; }

            var btnSo = new SerializedObject(btnRef);
            var layers = btnSo.FindProperty("_pressLayers");
            layers.intValue = ~0; // Everything — matches the other poke buttons.
            btnSo.ApplyModifiedProperties();
            Debug.Log($"[ButtonFixWirer] Glass spawn button '{btnRef.name}' _pressLayers → Everything.");
        }

        private static void WireGameControlsFallbackConfig()
        {
            var controls = Object.FindFirstObjectByType<GameControls>();
            if (controls == null) { Debug.LogError("[ButtonFixWirer] No GameControls in scene."); return; }

            // Reuse whatever config the clipboard is staging so both paths start the same night.
            var clipboard = Object.FindFirstObjectByType<NightClipboard>();
            NightConfigSO config = null;
            if (clipboard != null)
                config = new SerializedObject(clipboard).FindProperty("_config").objectReferenceValue as NightConfigSO;

            if (config == null)
            {
                // Fallback: first NightConfigSO in the project.
                var guids = AssetDatabase.FindAssets("t:NightConfigSO");
                if (guids.Length > 0)
                    config = AssetDatabase.LoadAssetAtPath<NightConfigSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            if (config == null) { Debug.LogError("[ButtonFixWirer] No NightConfigSO found to assign."); return; }

            var so = new SerializedObject(controls);
            so.FindProperty("_fallbackConfig").objectReferenceValue = config;
            so.ApplyModifiedProperties();
            Debug.Log($"[ButtonFixWirer] GameControls._fallbackConfig → '{config.name}'.");
        }
    }
}
#endif
