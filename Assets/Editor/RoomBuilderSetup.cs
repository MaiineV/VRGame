using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-shot setup: builds a closed room (4 walls + ceiling) around the bar play area,
/// sized to contain the spawn/seat/exit, and marks everything static for light baking.
/// The existing 20x20 Floor is left as-is. Walls are solid boxes so their inner faces
/// render from inside the room. Run from: Pour Decisions/Setup/Build Room.
/// </summary>
public static class RoomBuilderSetup
{
    private const float HalfX = 7f;
    private const float HalfZ = 7f;
    private const float Height = 3.5f;
    private const float Thick = 0.2f;
    private const string WallMatPath = "Assets/4. Prefabs/RoomWallMat.mat";

    [MenuItem("Pour Decisions/Setup/Build Room")]
    public static void Build()
    {
        var existing = GameObject.Find("Room");
        if (existing != null) Object.DestroyImmediate(existing);

        var room = new GameObject("Room");
        GameObjectUtility.SetStaticEditorFlags(room, (StaticEditorFlags)~0);

        var mat = GetWallMaterial();

        CreateBox(room.transform, "Wall_North", new Vector3(0f, Height * 0.5f, HalfZ), new Vector3(HalfX * 2f, Height, Thick), mat);
        CreateBox(room.transform, "Wall_South", new Vector3(0f, Height * 0.5f, -HalfZ), new Vector3(HalfX * 2f, Height, Thick), mat);
        CreateBox(room.transform, "Wall_East", new Vector3(HalfX, Height * 0.5f, 0f), new Vector3(Thick, Height, HalfZ * 2f), mat);
        CreateBox(room.transform, "Wall_West", new Vector3(-HalfX, Height * 0.5f, 0f), new Vector3(Thick, Height, HalfZ * 2f), mat);
        CreateBox(room.transform, "Ceiling", new Vector3(0f, Height, 0f), new Vector3(HalfX * 2f, Thick, HalfZ * 2f), mat);

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[RoomBuilderSetup] Room built ({HalfX * 2f}x{HalfZ * 2f}, h={Height}) and marked static.");
    }

    private static void CreateBox(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        GameObjectUtility.SetStaticEditorFlags(go, (StaticEditorFlags)~0);
    }

    private static Material GetWallMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(WallMatPath);
        if (mat != null) return mat;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        mat = new Material(shader) { color = new Color(0.52f, 0.46f, 0.4f) };
        AssetDatabase.CreateAsset(mat, WallMatPath);
        AssetDatabase.SaveAssets();
        return mat;
    }
}
