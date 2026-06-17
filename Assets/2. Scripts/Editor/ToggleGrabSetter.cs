using Gameplay.Interactions;
using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    /// <summary>
    /// One-shot: flips every SimpleVRGrabber in the open scene to toggle-grab mode
    /// (one press grabs, the next press drops) — gentler on the hand than holding the
    /// trigger the whole time. MCP can't reach the grabbers (they live on stripped
    /// prefab instances), so this does it via SerializedObject.
    /// </summary>
    public static class ToggleGrabSetter
    {
        [MenuItem("Pour Decisions/VR/Enable Toggle Grab (both hands)")]
        public static void Enable()
        {
            var grabbers = Object.FindObjectsByType<SimpleVRGrabber>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int n = 0;
            foreach (var g in grabbers)
            {
                var so = new SerializedObject(g);
                var p = so.FindProperty("_toggleGrab");
                if (p == null) continue;
                p.boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(g);
                n++;
            }
            Debug.Log($"[ToggleGrabSetter] Set _toggleGrab=true on {n} grabber(s).");
        }
    }
}
