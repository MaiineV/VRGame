#if UNITY_EDITOR
using System.IO;
using Data.SO;
using Gameplay.Customer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EditorTools
{
    /// <summary>
    /// Adds visible placeholder geometry (cubes/capsules) to the Bar scene and creates
    /// a Customer prefab + assigns it to every CustomerSO in Resources.
    /// Idempotent-ish: re-parents or updates existing placeholders by name.
    /// </summary>
    public static class VisualScaffolder
    {
        private const string PrefabsFolder = "Assets/Prefabs";
        private const string CustomerPrefabPath = "Assets/Prefabs/Customer.prefab";
        private const string CustomersFolder = "Assets/Resources/Database/Customers";

        [MenuItem("Pour Decisions/Visuals/Scaffold Placeholders")]
        public static void ScaffoldVisuals()
        {
            if (!Directory.Exists(PrefabsFolder)) Directory.CreateDirectory(PrefabsFolder);

            var customerPrefab = CreateOrGetCustomerPrefab();
            AssignCustomerPrefabToSOs(customerPrefab);

            var root = GameObject.Find("__BarSceneRoot");
            if (root == null)
            {
                Debug.LogError("[VisualScaffolder] __BarSceneRoot not found. Open the Bar scene first.");
                return;
            }

            BuildShelfBoard(root.transform);
            BuildChairs(root.transform);
            BuildServePointMarkers(root.transform);
            BuildSpawnExitMarkers(root.transform);
            BuildBackWall(root.transform);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[VisualScaffolder] Placeholders added.");
        }

        private static GameObject CreateOrGetCustomerPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(CustomerPrefabPath);
            if (existing != null) return existing;

            var root = new GameObject("Customer");

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0, 0.9f, 0);
            body.transform.localScale = new Vector3(0.4f, 0.6f, 0.4f);
            Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());

            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0, 1.7f, 0);
            head.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            Object.DestroyImmediate(head.GetComponent<BoxCollider>());

            // Physical hit volume (not trigger)
            var col = root.AddComponent<CapsuleCollider>();
            col.height = 1.8f; col.radius = 0.25f; col.direction = 1;
            col.center = new Vector3(0, 0.9f, 0);

            root.AddComponent<CustomerEntity>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, CustomerPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void AssignCustomerPrefabToSOs(GameObject prefab)
        {
            if (!Directory.Exists(CustomersFolder)) return;
            var guids = AssetDatabase.FindAssets("t:CustomerSO", new[] { CustomersFolder });
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var so = AssetDatabase.LoadAssetAtPath<CustomerSO>(path);
                if (so == null) continue;
                var serialized = new SerializedObject(so);
                var p = serialized.FindProperty("_prefab");
                if (p == null) continue;
                if (p.objectReferenceValue != prefab)
                {
                    p.objectReferenceValue = prefab;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(so);
                }
            }
        }

        private static void BuildShelfBoard(Transform root)
        {
            var name = "ShelfBoard";
            var existing = root.Find(name);
            if (existing != null) return;

            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = name;
            board.transform.SetParent(root, false);
            board.transform.position = new Vector3(0, 1.27f, -1.5f);
            board.transform.localScale = new Vector3(4f, 0.04f, 0.35f);
        }

        private static void BuildChairs(Transform root)
        {
            var seats = root.Find("Seats");
            if (seats == null) return;
            foreach (Transform seat in seats)
            {
                if (seat.Find("ChairVisual") != null) continue;
                var chair = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chair.name = "ChairVisual";
                chair.transform.SetParent(seat, false);
                chair.transform.localPosition = new Vector3(0, 0.45f, 0);
                chair.transform.localScale = new Vector3(0.45f, 0.9f, 0.45f);
            }
        }

        private static void BuildServePointMarkers(Transform root)
        {
            var seats = root.Find("Seats");
            if (seats == null) return;
            foreach (Transform seat in seats)
            {
                var serve = seat.Find("ServePoint");
                if (serve == null || serve.Find("Marker") != null) continue;
                var m = GameObject.CreatePrimitive(PrimitiveType.Cube);
                m.name = "Marker";
                m.transform.SetParent(serve, false);
                m.transform.localScale = new Vector3(0.12f, 0.003f, 0.12f);
                // Remove collider so it doesn't block drinks
                Object.DestroyImmediate(m.GetComponent<BoxCollider>());
                var rend = m.GetComponent<MeshRenderer>();
                if (rend != null && rend.sharedMaterial != null)
                {
                    var mat = new Material(rend.sharedMaterial);
                    mat.color = new Color(0.2f, 0.8f, 0.2f, 1f);
                    rend.sharedMaterial = mat;
                }
            }
        }

        private static void BuildSpawnExitMarkers(Transform root)
        {
            PlaceFlatMarker(root.Find("CustomerSpawnPoint"), new Color(0.2f, 0.6f, 1f));
            PlaceFlatMarker(root.Find("CustomerExitPoint"),  new Color(1f, 0.5f, 0.2f));
        }

        private static void PlaceFlatMarker(Transform point, Color color)
        {
            if (point == null || point.Find("Marker") != null) return;
            var m = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m.name = "Marker";
            m.transform.SetParent(point, false);
            m.transform.localPosition = new Vector3(0, 0.01f, 0);
            m.transform.localScale = new Vector3(0.4f, 0.02f, 0.4f);
            Object.DestroyImmediate(m.GetComponent<BoxCollider>());
            var rend = m.GetComponent<MeshRenderer>();
            if (rend != null && rend.sharedMaterial != null)
            {
                var mat = new Material(rend.sharedMaterial);
                mat.color = color;
                rend.sharedMaterial = mat;
            }
        }

        private static void BuildBackWall(Transform root)
        {
            if (root.Find("BackWall") != null) return;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "BackWall";
            wall.transform.SetParent(root, false);
            wall.transform.position = new Vector3(0, 1.5f, -2f);
            wall.transform.localScale = new Vector3(8f, 3f, 0.1f);
            var rend = wall.GetComponent<MeshRenderer>();
            if (rend != null && rend.sharedMaterial != null)
            {
                var mat = new Material(rend.sharedMaterial);
                mat.color = new Color(0.35f, 0.28f, 0.22f);
                rend.sharedMaterial = mat;
            }
        }
    }
}
#endif
