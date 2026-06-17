using TMPro;
using UnityEditor;
using UnityEngine;
using UI.Diegetic;

namespace EditorTools
{
    /// <summary>
    /// One-shot wirer: assigns the ProgressionBoard's _flashLabel to its child "FlashLabel" TMP
    /// (moved over from the old CashRegister). MCP update_component can't set Object references,
    /// so this does it via SerializedObject.
    /// </summary>
    public static class ProgressionBoardWirer
    {
        [MenuItem("Pour Decisions/Visuals/Wire ProgressionBoard Flash")]
        public static void Wire()
        {
            var board = Object.FindFirstObjectByType<ProgressionBoard>(FindObjectsInactive.Include);
            if (board == null)
            {
                Debug.LogError("[ProgressionBoardWirer] No ProgressionBoard found in scene.");
                return;
            }

            TMP_Text flash = null;
            foreach (var t in board.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t.gameObject.name == "FlashLabel") { flash = t; break; }
            }
            if (flash == null)
            {
                Debug.LogError("[ProgressionBoardWirer] No child TMP named 'FlashLabel' under ProgressionBoard.");
                return;
            }

            var so = new SerializedObject(board);
            var prop = so.FindProperty("_flashLabel");
            if (prop == null)
            {
                Debug.LogError("[ProgressionBoardWirer] _flashLabel field not found on ProgressionBoard.");
                return;
            }
            prop.objectReferenceValue = flash;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(board);
            Debug.Log("[ProgressionBoardWirer] Wired _flashLabel -> " + flash.name);
        }
    }
}
