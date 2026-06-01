using Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-shot: disable auto-start, add controller GameControls, and raise the player rig so the
/// player isn't crouched below the bar. Run: Pour Decisions/Setup/Configure Game Flow.
/// </summary>
public static class GameFlowSetup
{
    [MenuItem("Pour Decisions/Setup/Configure Game Flow")]
    public static void Configure()
    {
        var root = Object.FindAnyObjectByType<BarSceneRoot>();
        if (root != null)
        {
            var so = new SerializedObject(root);
            var p = so.FindProperty("_autoStartNight");
            if (p != null) { p.boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); }
            if (root.GetComponent<GameControls>() == null) root.gameObject.AddComponent<GameControls>();
            Debug.Log("[GameFlowSetup] _autoStartNight=false; GameControls added to BarSceneRoot.");
        }
        else Debug.LogWarning("[GameFlowSetup] BarSceneRoot not found.");

        // Player appeared crouched: FloorLevel tracking gave ~0 height. Raise the rig to a fixed
        // standing height. Tune this Y in the scene if it ends up too high/low on the headset.
        var anchor = GameObject.Find("PlayerAnchor");
        if (anchor != null)
        {
            var lp = anchor.transform.localPosition;
            anchor.transform.localPosition = new Vector3(lp.x, 1.3f, lp.z);
            Debug.Log($"[GameFlowSetup] PlayerAnchor raised to y=1.3 (was {lp.y}).");
        }
        else Debug.LogWarning("[GameFlowSetup] PlayerAnchor not found.");

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[GameFlowSetup] Done.");
    }
}
