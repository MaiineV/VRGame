#if UNITY_EDITOR
using Gameplay.Customer;
using UI.Diegetic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EditorTools
{
    /// <summary>
    /// Adds a <see cref="CustomerPatienceBar"/> to each customer seat and wires its seat
    /// reference. The bar builds its own world-space visual at runtime, so this is the only
    /// setup needed. Idempotent — re-running just re-points the seat reference.
    /// </summary>
    public static class PatienceBarWirer
    {
        [MenuItem("Pour Decisions/Wire Customer Patience Bars")]
        public static void Wire()
        {
            string[] seatNames = { "Seat_0", "Seat_1", "Seat_2" };
            int wired = 0;

            foreach (var seatName in seatNames)
            {
                var seatGo = GameObject.Find(seatName);
                if (seatGo == null) { Debug.LogError($"[PatienceBarWirer] {seatName} not found"); continue; }

                var seat = seatGo.GetComponent<CustomerSeatPoint>();
                if (seat == null) { Debug.LogError($"[PatienceBarWirer] {seatName} has no CustomerSeatPoint"); continue; }

                // Reuse an existing child rig if present, otherwise create one.
                var barT = seatGo.transform.Find("PatienceBarRig");
                CustomerPatienceBar bar;
                if (barT != null)
                {
                    bar = barT.GetComponent<CustomerPatienceBar>();
                    if (bar == null) bar = barT.gameObject.AddComponent<CustomerPatienceBar>();
                }
                else
                {
                    var rig = new GameObject("PatienceBarRig");
                    rig.transform.SetParent(seatGo.transform, false);
                    bar = rig.AddComponent<CustomerPatienceBar>();
                }

                var so = new SerializedObject(bar);
                so.FindProperty("_seat").objectReferenceValue = seat;
                so.ApplyModifiedProperties();
                wired++;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[PatienceBarWirer] Done — wired {wired} patience bar(s).");
        }
    }
}
#endif
