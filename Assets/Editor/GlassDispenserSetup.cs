using Gameplay.Interactions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-shot setup: drops a GlassDispenser on the bar and wires the Glass prefab into it.
/// MCP can't assign prefab object references, so this does it via SerializedObject.
/// Run from: Pour Decisions/Setup/Add Glass Dispenser.
/// </summary>
public static class GlassDispenserSetup
{
    private const string GlassPrefabPath = "Assets/4. Prefabs/Glass.prefab";

    [MenuItem("Pour Decisions/Setup/Add Glass Dispenser")]
    public static void Add()
    {
        var glassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GlassPrefabPath);
        if (glassPrefab == null)
        {
            Debug.LogError($"[GlassDispenserSetup] Glass prefab not found at {GlassPrefabPath}");
            return;
        }

        var existing = GameObject.Find("GlassDispenser");
        var go = existing != null ? existing : new GameObject("GlassDispenser");
        // On the bar, just to the side of the existing glass, within player reach.
        go.transform.position = new Vector3(-0.9f, 1.12f, 0.31f);
        go.transform.rotation = Quaternion.identity;

        var disp = go.GetComponent<GlassDispenser>();
        if (disp == null) disp = go.AddComponent<GlassDispenser>();

        var so = new SerializedObject(disp);
        var prop = so.FindProperty("_glassPrefab");
        prop.objectReferenceValue = glassPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(go.scene);
        EditorSceneManager.SaveScene(go.scene);
        Debug.Log("[GlassDispenserSetup] GlassDispenser placed at (-0.9, 1.12, 0.31) and Glass prefab assigned. Adjust its position in the scene if needed.");
    }
}
