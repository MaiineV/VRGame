#if UNITY_EDITOR
using Gameplay.Customer;
using TMPro;
using UI.Diegetic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EditorTools
{
    public static class BarUiWirer
    {
        [MenuItem("Pour Decisions/Wire Bar UI References")]
        public static void Wire()
        {
            WireRecipeMenuBoard();
            WireProgressionBoard();
            WireServeFeedbacks();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[BarUiWirer] Done — all Bar UI references wired.");
        }

        static void WireRecipeMenuBoard()
        {
            var go = GameObject.Find("RecipeMenuBoard");
            if (go == null) { Debug.LogError("[BarUiWirer] RecipeMenuBoard not found"); return; }
            var comp = go.GetComponent<RecipeMenuBoard>();
            var so = new SerializedObject(comp);
            so.FindProperty("_titleLabel").objectReferenceValue   = go.transform.Find("Title").GetComponent<TextMeshProUGUI>();
            so.FindProperty("_recipesLabel").objectReferenceValue = go.transform.Find("Recipes").GetComponent<TextMeshProUGUI>();
            so.ApplyModifiedProperties();
        }

        static void WireProgressionBoard()
        {
            var go = GameObject.Find("ProgressionBoard");
            if (go == null) { Debug.LogError("[BarUiWirer] ProgressionBoard not found"); return; }
            var comp = go.GetComponent<ProgressionBoard>();
            var so = new SerializedObject(comp);
            so.FindProperty("_nightLabel").objectReferenceValue = go.transform.Find("NightLabel").GetComponent<TextMeshProUGUI>();
            so.FindProperty("_cashLabel").objectReferenceValue  = go.transform.Find("CashLabel").GetComponent<TextMeshProUGUI>();
            so.FindProperty("_bestLabel").objectReferenceValue  = go.transform.Find("BestLabel").GetComponent<TextMeshProUGUI>();
            so.FindProperty("_starsLabel").objectReferenceValue = go.transform.Find("StarsLabel").GetComponent<TextMeshProUGUI>();
            so.ApplyModifiedProperties();
        }

        static void WireServeFeedbacks()
        {
            string[] seatNames = { "Seat_0", "Seat_1", "Seat_2" };
            foreach (var seatName in seatNames)
            {
                var seatGo = GameObject.Find(seatName);
                if (seatGo == null) { Debug.LogError($"[BarUiWirer] {seatName} not found"); continue; }

                var sfdT = seatGo.transform.Find("ServeFeedbackDisplay");
                if (sfdT == null) { Debug.LogError($"[BarUiWirer] {seatName}/ServeFeedbackDisplay not found"); continue; }

                var popupT  = sfdT.Find("PopupRoot");
                var labelT  = popupT?.Find("FeedbackText");

                var so = new SerializedObject(sfdT.GetComponent<ServeFeedbackDisplay>());
                so.FindProperty("_seat").objectReferenceValue        = seatGo.GetComponent<CustomerSeatPoint>();
                so.FindProperty("_popupRoot").objectReferenceValue   = popupT?.gameObject;
                so.FindProperty("_resultLabel").objectReferenceValue = labelT?.GetComponent<TextMeshProUGUI>();
                so.ApplyModifiedProperties();
            }
        }
    }
}
#endif
