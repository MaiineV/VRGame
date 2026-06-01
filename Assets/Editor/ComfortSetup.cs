using Gameplay;
using Gameplay.Interactions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// One-shot VR comfort pass: reset player height, lower move speed, add a speed-driven Vignette,
/// and add 72 Hz frame-rate lock. Run: Pour Decisions/Setup/Apply Comfort Fixes.
/// </summary>
public static class ComfortSetup
{
    [MenuItem("Pour Decisions/Setup/Apply Comfort Fixes")]
    public static void Apply()
    {
        // 1. Revert player height — don't stack a fixed offset on FloorLevel (breaks scale).
        var anchor = GameObject.Find("PlayerAnchor");
        if (anchor != null)
        {
            var lp = anchor.transform.localPosition;
            anchor.transform.localPosition = new Vector3(lp.x, 0f, lp.z);
            Debug.Log($"[ComfortSetup] PlayerAnchor Y reset to 0 (was {lp.y}).");
        }

        // 2. Lower smooth-move speed.
        var loco = Object.FindAnyObjectByType<ThumbstickLocomotion>();
        if (loco != null)
        {
            var so = new SerializedObject(loco);
            var sp = so.FindProperty("_speed");
            if (sp != null) { sp.floatValue = 1.2f; so.ApplyModifiedPropertiesWithoutUndo(); }
            Debug.Log("[ComfortSetup] Move speed set to 1.2.");
        }

        // 3. Comfort vignette on the global volume (create one if the scene has none).
        var volume = Object.FindAnyObjectByType<Volume>(FindObjectsInactive.Include);
        if (volume == null)
        {
            var go = new GameObject("ComfortVolume");
            volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/1. Scenes/Bar/Global Volume Profile.asset");
            if (prof == null)
            {
                prof = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(prof, "Assets/1. Scenes/Bar/ComfortVolumeProfile.asset");
                AssetDatabase.SaveAssets();
            }
            volume.sharedProfile = prof;
            Debug.Log("[ComfortSetup] Created global ComfortVolume.");
        }
        if (volume != null && volume.sharedProfile != null)
        {
            var profile = volume.sharedProfile;
            if (!profile.TryGet<Vignette>(out _))
            {
                var v = profile.Add<Vignette>(true);
                v.intensity.overrideState = true;
                v.intensity.value = 0f;
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
            }
            if (volume.GetComponent<ComfortVignette>() == null)
            {
                var cv = volume.gameObject.AddComponent<ComfortVignette>();
                var so = new SerializedObject(cv);
                var vp = so.FindProperty("_volume");
                if (vp != null) { vp.objectReferenceValue = volume; so.ApplyModifiedPropertiesWithoutUndo(); }
            }
            Debug.Log("[ComfortSetup] Vignette override + ComfortVignette added.");
        }
        else Debug.LogWarning("[ComfortSetup] No Volume found — comfort vignette skipped.");

        // 4. 72 Hz lock.
        var root = Object.FindAnyObjectByType<BarSceneRoot>();
        if (root != null && root.GetComponent<QuestPerformance>() == null)
        {
            root.gameObject.AddComponent<QuestPerformance>();
            Debug.Log("[ComfortSetup] QuestPerformance (72 Hz) added.");
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ComfortSetup] Done.");
    }
}
