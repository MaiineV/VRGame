using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// One-shot lighting setup for the closed bar room, plus a bake trigger.
/// The ceiling blocks the directional light, so interior point lights (Mixed: baked GI for
/// static geometry + realtime for dynamic glasses/customers) light the scene. Ambient is
/// raised so nothing is pitch black. Run "Setup Lighting" then "Bake Lighting Now".
/// </summary>
public static class LightingBakeSetup
{
    [MenuItem("Pour Decisions/Setup/Setup Lighting")]
    public static void Setup()
    {
        var existing = GameObject.Find("RoomLights");
        if (existing != null) Object.DestroyImmediate(existing);
        var root = new GameObject("RoomLights");

        AddPointLight(root.transform, "Light_Center", new Vector3(0f, 3.1f, 0f), new Color(1f, 0.92f, 0.8f), 11f, 6f);
        AddPointLight(root.transform, "Light_Bar", new Vector3(0f, 2.6f, -2.5f), new Color(1f, 0.86f, 0.62f), 9f, 5f);
        AddPointLight(root.transform, "Light_Spawn", new Vector3(-4f, 2.8f, 3f), new Color(0.85f, 0.9f, 1f), 9f, 4f);

        // Directional -> Mixed key light, softened (mostly blocked by the ceiling anyway).
        var dir = GameObject.Find("Directional Light");
        if (dir != null)
        {
            var l = dir.GetComponent<Light>();
            if (l != null) { l.lightmapBakeType = LightmapBakeType.Mixed; l.intensity = 0.5f; }
        }

        // Ambient floor so dynamic objects never go black.
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.34f, 0.34f, 0.38f);

        // Baking is on-demand by default in Unity 6 (the "Auto Generate" workflow was removed),
        // so there's nothing to set here — use "Bake Lighting Now" to trigger a bake.

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[LightingBakeSetup] Lighting configured (3 Mixed point lights + ambient). Run 'Bake Lighting Now'.");
    }

    [MenuItem("Pour Decisions/Setup/Bake Lighting Now")]
    public static void Bake()
    {
        Debug.Log("[LightingBakeSetup] Bake started (async). Watch the progress bar bottom-right.");
        Lightmapping.BakeAsync();
    }

    private static void AddPointLight(Transform parent, string name, Vector3 pos, Color color, float range, float intensity)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.range = range;
        l.intensity = intensity;
        // Fully Baked (not Mixed): zero realtime per-pixel cost on Quest, protecting the 72 Hz
        // frame budget (judder = nausea). Dynamic objects fall back to ambient light.
        l.lightmapBakeType = LightmapBakeType.Baked;
        l.shadows = LightShadows.None;
    }
}
