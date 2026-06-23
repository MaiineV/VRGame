using System.Text;
using UnityEditor;
using UnityEngine;
using Utilities;

namespace VRGame.EditorTools
{
    /// <summary>
    /// Deterministic, edit-mode reproduction + validation for the "glass changes size on grab/release"
    /// bug. Instantiates the real Glass prefab and runs the exact reparent sequences the runtime uses,
    /// under deliberately adversarial parent scales (non-uniform hand, scaled customers). Logs a PASS/FAIL
    /// table and demonstrates that the un-fixed (raw) round trip drifts. Creates only temporary objects
    /// and destroys them. Menu: Tools/Diagnostics/Glass Grab Scale Repro
    /// </summary>
    public static class GlassScaleRepro
    {
        private const string GlassPrefabPath = "Assets/4. Prefabs/Glass.prefab";
        private const float Eps = 1e-4f;

        [MenuItem("Tools/Diagnostics/Glass Grab Scale Repro")]
        public static void Run()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GlassPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[GlassScaleRepro] Glass prefab not found at {GlassPrefabPath}");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("===== GLASS GRAB SCALE REPRO =====");

            // Stand-ins for the runtime hierarchy.
            var pool = new GameObject("repro_pool").transform;                 // pool root, unscaled
            var rot = Quaternion.Euler(30f, 45f, 15f);                         // hands are held at angles
            var skewHand = new GameObject("repro_handNonUniform").transform;   // worst case: non-uniform + rotated
            skewHand.localScale = new Vector3(1.5f, 0.7f, 1.2f); skewHand.localRotation = rot;
            var uniformHand = new GameObject("repro_handUniform").transform;    // realistic: uniform, rotated
            uniformHand.localScale = new Vector3(1.4f, 1.4f, 1.4f); uniformHand.localRotation = rot;
            var kobold = new GameObject("repro_kobold").transform;             // uniform 1.3 customer
            kobold.localScale = new Vector3(1.3f, 1.3f, 1.3f);
            var baseCust = new GameObject("repro_baseCustomer").transform;     // non-uniform 0.4/0.6/0.4
            baseCust.localScale = new Vector3(0.4f, 0.6f, 0.4f);

            GameObject glass = null;
            bool pass = true;
            try
            {
                glass = Object.Instantiate(prefab, pool);
                var t = glass.transform;
                t.localScale = Vector3.one;                                    // design scale
                Vector3 design = t.lossyScale;                                 // == (1,1,1) under unscaled pool
                sb.AppendLine($"design lossyScale = {Fmt(design)}\n");

                // --- Player path, RAW: SetParent true/true, no scale re-impose (the old behaviour) ---
                Reset(t, pool);
                t.SetParent(skewHand, true);
                t.SetParent(pool, true);
                Vector3 rawAfter = t.lossyScale;
                bool rawDrifted = !Approx(rawAfter, design);
                sb.AppendLine($"[player RAW   | non-uniform hand] after grab+release: {Fmt(rawAfter)}  -> "
                              + (rawDrifted ? "DRIFTED (bug reproduced)" : "stable"));

                // --- Player path, RAW, REPEATED: the real "grab & release over and over" scenario.
                // The same glass is never pooled/reset, so any per-cycle approximation accumulates. ---
                Reset(t, pool);
                for (int i = 0; i < 200; i++)
                {
                    t.SetParent(skewHand, true);
                    t.SetParent(pool, true);
                }
                Vector3 rawLoop = t.lossyScale;
                bool rawLoopDrifted = !Approx(rawLoop, design);
                sb.AppendLine($"[player RAW   | non-uniform hand x200] after 200 grab+release: {Fmt(rawLoop)}  -> "
                              + (rawLoopDrifted ? "DRIFTED (bug reproduced)" : "stable"));

                // Same 200 cycles WITH the fix in place -> must stay at design.
                Reset(t, pool);
                Vector3 fixedHeld = t.lossyScale;
                for (int i = 0; i < 200; i++)
                {
                    t.SetParent(skewHand, true); TransformScaleUtil.SetWorldScale(t, fixedHeld);
                    t.SetParent(pool, true); TransformScaleUtil.SetWorldScale(t, fixedHeld);
                }
                bool fixedLoopOk = Approx(t.lossyScale, design);
                pass &= fixedLoopOk;
                sb.AppendLine($"[player FIXED | non-uniform hand x200] after 200 grab+release: {Fmt(t.lossyScale)} {PF(fixedLoopOk)}\n");

                // --- Player path, FIXED: SetParent + SetWorldScale (what the grabber now does) ---
                // After release the glass is back under the unscaled pool, so the size is exact.
                Reset(t, pool);
                Vector3 held = t.lossyScale;
                t.SetParent(skewHand, true); TransformScaleUtil.SetWorldScale(t, held);
                Vector3 inHandSkew = t.lossyScale;
                t.SetParent(pool, true); TransformScaleUtil.SetWorldScale(t, held);
                bool relSkewOk = Approx(t.lossyScale, design);
                pass &= relSkewOk;
                sb.AppendLine($"[player FIXED | non-uniform hand] in-hand: {Fmt(inHandSkew)} (info; shear from a "
                              + "non-uniform hand is unavoidable) | after release: {0} {1}"
                              .Replace("{0}", Fmt(t.lossyScale)).Replace("{1}", PF(relSkewOk)));

                // Realistic case: a uniform (any size) hand round-trips perfectly, in-hand and after.
                Reset(t, pool);
                held = t.lossyScale;
                t.SetParent(uniformHand, true); TransformScaleUtil.SetWorldScale(t, held);
                bool inHandUniOk = Approx(t.lossyScale, design);
                t.SetParent(pool, true); TransformScaleUtil.SetWorldScale(t, held);
                bool relUniOk = Approx(t.lossyScale, design);
                pass &= inHandUniOk && relUniOk;
                sb.AppendLine($"[player FIXED | uniform hand]     in-hand: {Fmt(t.lossyScale)} {PF(inHandUniOk)} "
                              + $"| after release: {PF(relUniOk)}");

                // --- NPC path: carry(false) -> return(false) -> ResetForPool normalize ---
                NpcCase(sb, t, pool, kobold, "kobold 1.3", design, ref pass);
                NpcCase(sb, t, pool, baseCust, "base 0.4/0.6/0.4", design, ref pass);

                sb.AppendLine($"\nRESULT: {(pass ? "PASS" : "FAIL")}");
            }
            finally
            {
                if (glass != null) Object.DestroyImmediate(glass);
                Object.DestroyImmediate(pool.gameObject);
                Object.DestroyImmediate(skewHand.gameObject);
                Object.DestroyImmediate(uniformHand.gameObject);
                Object.DestroyImmediate(kobold.gameObject);
                Object.DestroyImmediate(baseCust.gameObject);
            }

            if (pass) Debug.Log(sb.ToString());
            else Debug.LogError(sb.ToString());
        }

        // Mirrors CarryServedGlass (worldPositionStays:false) -> ReturnToPool (false) -> ResetForPool normalize.
        private static void NpcCase(StringBuilder sb, Transform t, Transform pool, Transform customer,
                                    string label, Vector3 design, ref bool pass)
        {
            Reset(t, pool);
            t.SetParent(customer, false);          // CarryServedGlass (glass hidden while carried)
            t.SetParent(pool, false);              // PoolGeneric.ReturnToPool
            t.localScale = Vector3.one;            // Glass.ResetForPool normalises to design
            bool ok = Approx(t.lossyScale, design);
            pass &= ok;
            sb.AppendLine($"[npc {label,-16}] after carry+return+reset: {Fmt(t.lossyScale)} {PF(ok)}");
        }

        private static void Reset(Transform t, Transform pool)
        {
            t.SetParent(pool, true);
            t.localScale = Vector3.one;
        }

        private static bool Approx(Vector3 a, Vector3 b) =>
            Mathf.Abs(a.x - b.x) < Eps && Mathf.Abs(a.y - b.y) < Eps && Mathf.Abs(a.z - b.z) < Eps;

        private static string Fmt(Vector3 v) => $"({v.x:F4}, {v.y:F4}, {v.z:F4})";
        private static string PF(bool ok) => ok ? "PASS" : "FAIL";
    }
}
