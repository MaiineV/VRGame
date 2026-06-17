#if UNITY_EDITOR
using Gameplay.Interactions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EditorTools
{
    /// <summary>
    /// Applies the VR comfort defaults to the locomotion instance already placed in the scene
    /// (its serialized values otherwise override the script defaults): switch to Teleport mode
    /// and drop the smooth-move speed. The MSAA/foveation changes live in the RP asset and
    /// QuestPerformance respectively, so this only touches the scene component.
    /// </summary>
    public static class ComfortWirer
    {
        [MenuItem("Pour Decisions/Apply VR Comfort Settings")]
        public static void Apply()
        {
            var loco = Object.FindFirstObjectByType<ThumbstickLocomotion>();
            if (loco == null) { Debug.LogError("[ComfortWirer] No ThumbstickLocomotion in scene."); return; }

            var so = new SerializedObject(loco);
            so.FindProperty("_mode").enumValueIndex = 1; // 0 = Smooth, 1 = Teleport
            so.FindProperty("_speed").floatValue = 1.0f;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[ComfortWirer] Locomotion set to Teleport, smooth speed 1.0.");
        }
    }
}
#endif
