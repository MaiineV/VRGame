using Gameplay.Customer;
using TMPro;
using UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-shot setup: builds a world-space order label (canvas + TMP) per customer seat and
/// wires it to the seat. MCP can't create canvases / assign object refs, so this does it.
/// Run from: Pour Decisions/Setup/Add Customer Order Labels.
/// </summary>
public static class CustomerOrderLabelSetup
{
    [MenuItem("Pour Decisions/Setup/Add Customer Order Labels")]
    public static void Add()
    {
        var seats = Object.FindObjectsByType<CustomerSeatPoint>(FindObjectsSortMode.None);
        if (seats == null || seats.Length == 0)
        {
            Debug.LogError("[CustomerOrderLabelSetup] No CustomerSeatPoint found in scene.");
            return;
        }

        foreach (var seat in seats)
        {
            string objName = $"OrderLabel_Seat{seat.Index}";
            var prev = GameObject.Find(objName);
            if (prev != null) Object.DestroyImmediate(prev);

            // World-space canvas
            var canvasGO = new GameObject(objName, typeof(Canvas), typeof(CustomerOrderLabel));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)canvasGO.transform;
            rt.sizeDelta = new Vector2(300f, 140f);
            canvasGO.transform.position = seat.transform.position + new Vector3(0f, 2.6f, 0f);
            canvasGO.transform.localScale = Vector3.one * 0.004f; // ~1.2 m wide

            // Text child (this is the visual we toggle when the seat is empty)
            var textGO = new GameObject("Text", typeof(TextMeshProUGUI));
            textGO.transform.SetParent(canvasGO.transform, false);
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            var trt = tmp.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 12f; tmp.fontSizeMax = 80f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.text = "?";
            tmp.color = Color.white;

            // Wire the component
            var label = canvasGO.GetComponent<CustomerOrderLabel>();
            var so = new SerializedObject(label);
            so.FindProperty("_seat").objectReferenceValue = seat;
            so.FindProperty("_label").objectReferenceValue = tmp;
            so.FindProperty("_root").objectReferenceValue = textGO; // hide text (not canvas) when empty
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"[CustomerOrderLabelSetup] Created {objName} (seat index {seat.Index}).");
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[CustomerOrderLabelSetup] Done. Adjust _headOffset / scale per taste.");
    }
}
