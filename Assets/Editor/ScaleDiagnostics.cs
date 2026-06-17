using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace VRGame.EditorTools
{
    /// <summary>
    /// READ-ONLY scale audit for the VR scene. Logs world-space sizes (metres) of key
    /// furniture, the computed player eye height, and kobold heights. Modifies nothing.
    /// Menu: Tools/Diagnostics/Scale Report
    /// </summary>
    public static class ScaleDiagnostics
    {
        [MenuItem("Tools/Diagnostics/Scale Report")]
        public static void Report()
        {
            var sb = new StringBuilder();
            sb.AppendLine("===== SCALE REPORT =====");

            // --- Player rig ---
            var rig = Object.FindAnyObjectByType<OVRCameraRig>();
            if (rig != null)
            {
                var rigT = rig.transform;
                sb.AppendLine($"[Player] OVRCameraRig world pos = {rigT.position}  lossyScale = {rigT.lossyScale}");
                if (rig.centerEyeAnchor != null)
                    sb.AppendLine($"[Player] CenterEyeAnchor world pos = {rig.centerEyeAnchor.position} (editor pose; runtime adds real headset height with FloorLevel)");
                sb.AppendLine($"[Player] With FloorLevel tracking, runtime EYE height ~= rig.y + ~1.6m = {rigT.position.y + 1.6f:F2}m");

                var mgr = Object.FindAnyObjectByType<OVRManager>();
                if (mgr != null)
                    sb.AppendLine($"[Player] OVRManager.trackingOriginType = {mgr.trackingOriginType}");
            }
            else sb.AppendLine("[Player] No OVRCameraRig found.");

            // --- Furniture & environment by name ---
            string[] keys = { "bar", "counter", "stool", "chair", "seat", "table",
                              "floor", "ground", "wall", "shelf", "bottle", "glass" };
            sb.AppendLine("\n[Furniture] name | worldCenter.y | size(WxHxD) metres");
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var rows = new List<(string key, string line, float h)>();
            foreach (var r in renderers)
            {
                string lower = r.gameObject.name.ToLowerInvariant();
                string match = keys.FirstOrDefault(k => lower.Contains(k));
                if (match == null) continue;
                var b = r.bounds; // world-space AABB
                rows.Add((match,
                    $"  {r.gameObject.name,-28} | yC={b.center.y,6:F2} | {b.size.x,5:F2} x {b.size.y,5:F2} x {b.size.z,5:F2}",
                    b.size.y));
            }
            foreach (var grp in rows.GroupBy(x => x.key).OrderBy(g => g.Key))
            {
                sb.AppendLine($" -- {grp.Key} --");
                foreach (var row in grp.OrderByDescending(x => x.h).Take(6))
                    sb.AppendLine(row.line);
            }

            // --- Seat points ---
            var seatType = System.Type.GetType("Gameplay.Customer.CustomerSeatPoint, Assembly-CSharp");
            if (seatType != null)
            {
                var seats = Object.FindObjectsByType(seatType, FindObjectsSortMode.None);
                sb.AppendLine($"\n[Seats] {seats.Length} CustomerSeatPoint(s):");
                foreach (var s in seats.Cast<Component>())
                    sb.AppendLine($"  {s.gameObject.name,-24} world pos = {s.transform.position}");
            }

            // --- Kobold customers (by total renderer bounds height) ---
            sb.AppendLine("\n[Customers] name | total mesh height (m) | feet.y .. head.y");
            foreach (var r in renderers)
            {
                string lower = r.gameObject.name.ToLowerInvariant();
                if (!(lower.Contains("kobold") || lower.Contains("customer") || lower.Contains("cc_base")))
                    continue;
                // climb to a Customer root if present
                var t = r.transform;
                var rends = t.root.GetComponentsInChildren<Renderer>();
                var bounds = rends[0].bounds;
                foreach (var rr in rends) bounds.Encapsulate(rr.bounds);
                sb.AppendLine($"  {t.root.name,-28} | H={bounds.size.y,5:F2} | {bounds.min.y,5:F2} .. {bounds.max.y,5:F2}");
            }

            Debug.Log(sb.ToString());
        }

        [MenuItem("Tools/Diagnostics/Bar Hierarchy Dump")]
        public static void BarTree()
        {
            string[] keys = { "bar", "counter", "shelf", "bottle", "glass", "serve", "pour", "stool" };
            var sb = new StringBuilder();
            sb.AppendLine("===== BAR HIERARCHY (path | worldPos | localPos | scale | hasRenderer) =====");

            var all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            // Print any transform whose own name OR any ancestor name matches a key.
            foreach (var t in all)
            {
                bool match = false;
                for (var p = t; p != null; p = p.parent)
                {
                    string n = p.name.ToLowerInvariant();
                    if (keys.Any(k => n.Contains(k))) { match = true; break; }
                }
                if (!match) continue;

                bool hasR = t.GetComponent<Renderer>() != null;
                var wp = t.position;
                var lp = t.localPosition;
                sb.AppendLine($"{Path(t),-44} | w({wp.x,6:F2},{wp.y,6:F2},{wp.z,6:F2}) | l({lp.x,6:F2},{lp.y,6:F2},{lp.z,6:F2}) | s{t.localScale} | {(hasR ? "MESH" : "")}");
            }
            Debug.Log(sb.ToString());
        }

        private static string Path(Transform t)
        {
            var stack = new List<string>();
            for (var p = t; p != null; p = p.parent) stack.Add(p.name);
            stack.Reverse();
            return string.Join("/", stack);
        }
    }
}
