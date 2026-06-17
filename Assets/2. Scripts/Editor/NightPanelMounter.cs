using Gameplay.Interactions;
using UI.Diegetic;
using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    /// <summary>
    /// One-shot: turns the grabbable NightClipboard into a fixed wall panel by stripping its
    /// grab/physics components (GrabBridge, Collider(s), Rigidbody). The ClipboardMesh stays as
    /// the wall plate and the PokeButtons keep working (poked directly, _requireHeld is false).
    /// Positioning is done separately via MCP set_transform.
    /// </summary>
    public static class NightPanelMounter
    {
        [MenuItem("Pour Decisions/Visuals/Mount NightClipboard On Wall")]
        public static void Mount()
        {
            var clip = Object.FindFirstObjectByType<NightClipboard>(FindObjectsInactive.Include);
            if (clip == null)
            {
                Debug.LogError("[NightPanelMounter] No NightClipboard found in scene.");
                return;
            }

            var go = clip.gameObject;
            int removed = 0;

            // GrabBridge first (it likely RequireComponent(Rigidbody/Collider)).
            var grab = go.GetComponent<GrabBridge>();
            if (grab != null) { Object.DestroyImmediate(grab); removed++; }

            foreach (var col in go.GetComponents<Collider>())
            {
                Object.DestroyImmediate(col);
                removed++;
            }

            var rb = go.GetComponent<Rigidbody>();
            if (rb != null) { Object.DestroyImmediate(rb); removed++; }

            EditorUtility.SetDirty(go);
            Debug.Log($"[NightPanelMounter] Stripped {removed} grab/physics component(s); NightClipboard is now a static wall panel.");
        }
    }
}
