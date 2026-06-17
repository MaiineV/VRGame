using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Creates a Light Probe Group covering the bar play area. With the room lit by fully-baked
/// lights, dynamic objects (customers, glasses) otherwise fall back to flat ambient and look
/// pasted-on. Light probes capture the baked lighting at a grid of points so moving objects
/// get believable directional light/shadowing. Run this, then "Bake Lighting Now" (probes are
/// baked together with the lightmaps). Menu: Pour Decisions/Setup/Setup Light Probes.
/// </summary>
public static class LightProbeSetup
{
    // Play-area bounds (world units). Tuned to the closed bar room: customers approach the bar
    // (~z -2.5) from the spawn side (~z +3), within roughly x [-4, 4].
    private static readonly float[] Xs = { -4f, -2f, 0f, 2f, 4f };
    private static readonly float[] Zs = { -3f, -1f, 1f, 3f };
    private static readonly float[] Ys = { 0.4f, 1.2f, 2.1f }; // floor pickup, torso/glass, head height

    [MenuItem("Pour Decisions/Setup/Setup Light Probes")]
    public static void Setup()
    {
        var existing = GameObject.Find("LightProbes");
        if (existing != null) Object.DestroyImmediate(existing);

        var go = new GameObject("LightProbes");
        var group = go.AddComponent<LightProbeGroup>();

        var positions = new System.Collections.Generic.List<Vector3>(Xs.Length * Zs.Length * Ys.Length);
        foreach (var y in Ys)
            foreach (var z in Zs)
                foreach (var x in Xs)
                    positions.Add(new Vector3(x, y, z));

        group.probePositions = positions.ToArray();

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[LightProbeSetup] Created LightProbeGroup with {positions.Count} probes. " +
                  "Now run 'Pour Decisions/Setup/Bake Lighting Now' to bake them.");
    }
}
