#if UNITY_EDITOR
using Gameplay.Customer;
using Gameplay.Interactions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EditorTools
{
    public static class ServeSocketWirer
    {
        [MenuItem("Pour Decisions/Visuals/Wire Serve Sockets")]
        public static void Wire()
        {
            var root = GameObject.Find("__BarSceneRoot");
            if (root == null) { Debug.LogError("[ServeSocketWirer] __BarSceneRoot not found."); return; }
            var seatsNode = root.transform.Find("Seats");
            if (seatsNode == null) { Debug.LogError("[ServeSocketWirer] Seats node not found."); return; }

            int wired = 0;
            foreach (Transform seatT in seatsNode)
            {
                var seat = seatT.GetComponent<CustomerSeatPoint>();
                if (seat == null) continue;

                var serve = seat.transform.Find("ServePoint");
                if (serve == null) continue;
                var socket = serve.GetComponent<ServeSocket>();
                if (socket == null) continue;

                var so = new SerializedObject(seat);
                var p = so.FindProperty("_serveSocket");
                if (p == null) continue;
                if (p.objectReferenceValue != socket)
                {
                    p.objectReferenceValue = socket;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    wired++;
                }
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[ServeSocketWirer] Wired {wired} seats.");
        }
    }
}
#endif
