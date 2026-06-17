#if UNITY_EDITOR
using Gameplay.Customer;
using UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EditorTools
{
    public static class TicketViewScaffolder
    {
        [MenuItem("Pour Decisions/Visuals/Scaffold Ticket Views")]
        public static void Scaffold()
        {
            var root = GameObject.Find("__BarSceneRoot");
            if (root == null)
            {
                Debug.LogError("[TicketViewScaffolder] __BarSceneRoot not found.");
                return;
            }

            var seats = root.transform.Find("Seats");
            if (seats == null)
            {
                Debug.LogError("[TicketViewScaffolder] Seats node not found.");
                return;
            }

            int created = 0;
            TicketView[] allTickets = new TicketView[seats.childCount];
            int idx = 0;

            foreach (Transform seat in seats)
            {
                var seatPoint = seat.GetComponent<CustomerSeatPoint>();
                if (seatPoint == null) { idx++; continue; }

                var existing = seat.GetComponentInChildren<TicketView>(true);
                if (existing != null) { allTickets[idx++] = existing; continue; }

                var ticketGo = new GameObject("TicketView");
                ticketGo.transform.SetParent(seat, false);
                ticketGo.transform.localPosition = new Vector3(0, 2.2f, 0);

                var canvas = ticketGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                var rt = ticketGo.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0.3f, 0.12f);
                ticketGo.transform.localScale = Vector3.one;

                ticketGo.AddComponent<CanvasScaler>();

                var bgGo = new GameObject("Background");
                bgGo.transform.SetParent(ticketGo.transform, false);
                var bgRt = bgGo.AddComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;
                var bgImg = bgGo.AddComponent<Image>();
                bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

                var labelGo = new GameObject("RecipeLabel");
                labelGo.transform.SetParent(ticketGo.transform, false);
                var labelRt = labelGo.AddComponent<RectTransform>();
                labelRt.anchorMin = new Vector2(0.05f, 0.4f);
                labelRt.anchorMax = new Vector2(0.95f, 0.95f);
                labelRt.offsetMin = Vector2.zero;
                labelRt.offsetMax = Vector2.zero;
                var tmp = labelGo.AddComponent<TextMeshProUGUI>();
                tmp.text = "—";
                tmp.fontSize = 0.06f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.enableAutoSizing = false;

                var barBgGo = new GameObject("PatienceBarBg");
                barBgGo.transform.SetParent(ticketGo.transform, false);
                var barBgRt = barBgGo.AddComponent<RectTransform>();
                barBgRt.anchorMin = new Vector2(0.05f, 0.08f);
                barBgRt.anchorMax = new Vector2(0.95f, 0.32f);
                barBgRt.offsetMin = Vector2.zero;
                barBgRt.offsetMax = Vector2.zero;
                var barBgImg = barBgGo.AddComponent<Image>();
                barBgImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);

                var barFillGo = new GameObject("PatienceFill");
                barFillGo.transform.SetParent(barBgGo.transform, false);
                var barFillRt = barFillGo.AddComponent<RectTransform>();
                barFillRt.anchorMin = Vector2.zero;
                barFillRt.anchorMax = Vector2.one;
                barFillRt.offsetMin = Vector2.zero;
                barFillRt.offsetMax = Vector2.zero;
                var fillImg = barFillGo.AddComponent<Image>();
                fillImg.color = new Color(0.2f, 0.8f, 0.2f, 1f);
                fillImg.type = Image.Type.Filled;
                fillImg.fillMethod = Image.FillMethod.Horizontal;
                fillImg.fillAmount = 1f;

                var tv = ticketGo.AddComponent<TicketView>();
                var so = new SerializedObject(tv);
                so.FindProperty("_seat").objectReferenceValue = seatPoint;
                so.FindProperty("_root").objectReferenceValue = ticketGo;
                so.FindProperty("_recipeLabel").objectReferenceValue = tmp;
                so.FindProperty("_patienceFill").objectReferenceValue = fillImg;
                so.ApplyModifiedPropertiesWithoutUndo();

                allTickets[idx] = tv;
                created++;
                idx++;
            }

            ScaffoldOrdersBoard(root.transform, allTickets);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[TicketViewScaffolder] Created {created} ticket views.");
        }

        private static void ScaffoldOrdersBoard(Transform root, TicketView[] tickets)
        {
            if (root.Find("OrdersBoard") != null) return;

            var boardGo = new GameObject("OrdersBoard");
            boardGo.transform.SetParent(root, false);
            boardGo.transform.position = new Vector3(-1.8f, 1.8f, -1.9f);
            boardGo.transform.rotation = Quaternion.Euler(0, 90, 0);

            var canvas = boardGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = boardGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0.5f, 0.3f);

            boardGo.AddComponent<CanvasScaler>();

            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(boardGo.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            var timerGo = new GameObject("TimerLabel");
            timerGo.transform.SetParent(boardGo.transform, false);
            var timerRt = timerGo.AddComponent<RectTransform>();
            timerRt.anchorMin = new Vector2(0.05f, 0.65f);
            timerRt.anchorMax = new Vector2(0.95f, 0.95f);
            timerRt.offsetMin = Vector2.zero;
            timerRt.offsetMax = Vector2.zero;
            var timerTmp = timerGo.AddComponent<TextMeshProUGUI>();
            timerTmp.text = "--:--";
            timerTmp.fontSize = 0.08f;
            timerTmp.alignment = TextAlignmentOptions.Center;
            timerTmp.color = Color.white;

            var statsGo = new GameObject("StatsLabel");
            statsGo.transform.SetParent(boardGo.transform, false);
            var statsRt = statsGo.AddComponent<RectTransform>();
            statsRt.anchorMin = new Vector2(0.05f, 0.1f);
            statsRt.anchorMax = new Vector2(0.95f, 0.55f);
            statsRt.offsetMin = Vector2.zero;
            statsRt.offsetMax = Vector2.zero;
            var statsTmp = statsGo.AddComponent<TextMeshProUGUI>();
            statsTmp.text = "Sales 0   Cash $0";
            statsTmp.fontSize = 0.05f;
            statsTmp.alignment = TextAlignmentOptions.Center;
            statsTmp.color = new Color(0.8f, 0.8f, 0.8f);

            var board = boardGo.AddComponent<OrdersBoard>();
            var so = new SerializedObject(board);
            so.FindProperty("_timerLabel").objectReferenceValue = timerTmp;
            so.FindProperty("_statsLabel").objectReferenceValue = statsTmp;

            var rowsProp = so.FindProperty("_rows");
            int validCount = 0;
            foreach (var t in tickets) if (t != null) validCount++;
            rowsProp.arraySize = validCount;
            int i = 0;
            foreach (var t in tickets)
            {
                if (t == null) continue;
                rowsProp.GetArrayElementAtIndex(i++).objectReferenceValue = t;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
