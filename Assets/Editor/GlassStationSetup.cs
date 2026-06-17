using Gameplay.Interactions;
using UI.Diegetic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-shot scene setup for the limited-glass station: builds a diegetic poke button that
/// spawns glasses and a trash bin that recycles them, then wires the spawn button into the
/// existing GlassDispenser. Run: Pour Decisions/Setup/Setup Glass Station.
/// </summary>
public static class GlassStationSetup
{
    // World positions, tuned to the bar (dispenser sits at ~(-0.9, 1.12, 0.31)).
    // Button + bin both sit ON the bar top (y ~1.12) within easy reach of the spawn spot.
    private static readonly Vector3 ButtonPos = new Vector3(-0.62f, 1.13f, 0.31f);
    private static readonly Vector3 TrashPos = new Vector3(-0.3f, 1.12f, 0.31f);

    [MenuItem("Pour Decisions/Setup/Setup Glass Station")]
    public static void Setup()
    {
        var dispenser = Object.FindAnyObjectByType<GlassDispenser>();
        if (dispenser == null)
        {
            Debug.LogError("[GlassStationSetup] No GlassDispenser in the scene. Add one first.");
            return;
        }

        var button = BuildSpawnButton();
        BuildTrashBin();

        // Wire the button into the dispenser.
        var so = new SerializedObject(dispenser);
        var prop = so.FindProperty("_spawnButton");
        if (prop != null) { prop.objectReferenceValue = button; so.ApplyModifiedPropertiesWithoutUndo(); }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[GlassStationSetup] Spawn button + trash bin built and wired. " +
                  "Press the button (or controller X) to dispense; drop glasses in the bin to recycle.");
    }

    private static PokeButton BuildSpawnButton()
    {
        var existing = GameObject.Find("SpawnGlassButton");
        if (existing != null) Object.DestroyImmediate(existing);

        var root = new GameObject("SpawnGlassButton");
        root.transform.position = ButtonPos;

        // Press zone (trigger) lives on the root alongside PokeButton.
        var zone = root.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.size = new Vector3(0.06f, 0.05f, 0.06f);
        zone.center = new Vector3(0f, 0.02f, 0f);

        // Visual base.
        var baseGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseGo.name = "Base";
        baseGo.transform.SetParent(root.transform, false);
        baseGo.transform.localPosition = new Vector3(0f, -0.006f, 0f);
        baseGo.transform.localScale = new Vector3(0.07f, 0.012f, 0.07f);
        StripCollider(baseGo);
        Paint(baseGo, new Color(0.15f, 0.15f, 0.18f));

        // Visual cap (depresses on press).
        var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cap.name = "Cap";
        cap.transform.SetParent(root.transform, false);
        cap.transform.localPosition = new Vector3(0f, 0.006f, 0f);
        cap.transform.localScale = new Vector3(0.05f, 0.008f, 0.05f);
        StripCollider(cap);
        Paint(cap, new Color(0.25f, 0.80f, 0.30f)); // green "dispense"

        var poke = root.AddComponent<PokeButton>();
        var so = new SerializedObject(poke);
        SetRef(so, "_cap", cap.transform);
        int hand = LayerMask.NameToLayer("Hand");
        if (hand >= 0) so.FindProperty("_pressLayers").intValue = 1 << hand; // else default ~0
        so.ApplyModifiedPropertiesWithoutUndo();

        return poke;
    }

    private static void BuildTrashBin()
    {
        var existing = GameObject.Find("TrashBin");
        if (existing != null) Object.DestroyImmediate(existing);

        var root = new GameObject("TrashBin");
        root.transform.position = TrashPos;

        // Tabletop bin: a short, clearly-visible cylinder sitting on the bar so the player can
        // drop glasses in at working height (no bending down). Solid body so glasses rest in it.
        var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "BinBody";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        body.transform.localScale = new Vector3(0.14f, 0.12f, 0.14f); // ~0.24 m tall, 0.14 wide
        Paint(body, new Color(0.45f, 0.47f, 0.52f)); // light steel — readable under baked light

        // Bright rim so it reads as a receptacle / "throw here".
        var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.name = "Rim";
        rim.transform.SetParent(root.transform, false);
        rim.transform.localPosition = new Vector3(0f, 0.235f, 0f);
        rim.transform.localScale = new Vector3(0.16f, 0.01f, 0.16f);
        StripCollider(rim);
        Paint(rim, new Color(0.90f, 0.30f, 0.20f)); // red rim

        // Trigger mouth at the top: releasing a glass here recycles it.
        var mouth = new GameObject("Mouth");
        mouth.transform.SetParent(root.transform, false);
        mouth.transform.localPosition = Vector3.zero;
        var col = mouth.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.center = new Vector3(0f, 0.26f, 0f);
        col.size = new Vector3(0.2f, 0.18f, 0.2f);
        mouth.AddComponent<GlassTrashBin>();
    }

    private static void SetRef(SerializedObject so, string prop, Object value)
    {
        var p = so.FindProperty(prop);
        if (p != null) p.objectReferenceValue = value;
    }

    private static void StripCollider(GameObject go)
    {
        var c = go.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);
    }

    private static void Paint(GameObject go, Color color)
    {
        var rend = go.GetComponent<Renderer>();
        if (rend == null) return;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader) { color = color };
        rend.sharedMaterial = mat;
    }
}
