using UnityEditor;
using UnityEngine;

namespace VRGame.EditorTools
{
    /// <summary>
    /// One-shot import optimizer for the two Kobold customer FBX models.
    ///
    /// Run via: Tools > Optimize > Kobold Import
    ///
    /// Phase 1 - REPORT: logs triangle count and vertex count for each model so
    ///           you can decide if mesh decimation is needed (Quest 2 budget:
    ///           ~15-20 k tris per character with up to 3 on screen).
    ///
    /// Phase 2 - OPTIMIZE: applies Quest 2-safe ModelImporter settings and calls
    ///           SaveAndReimport(). Safe to re-run (all assignments are idempotent).
    ///
    /// Rig / animation rationale:
    ///   - animationType is kept as Generic (2). optimizeGameObjects = true
    ///     requires a non-None rig to collapse the 85-bone CC_Base hierarchy.
    ///     Setting the rig to None would disable optimizeGameObjects entirely.
    ///   - importAnimation is set to FALSE. The FBX contains only a T-pose and
    ///     one stray take; no Animator drives these NPCs. Skipping animation
    ///     import avoids compiling useless AnimationClip assets and saves import time.
    ///   - extraExposedTransformPaths is forced to empty: no Animator sub-bone
    ///     lookups are needed, so the full hierarchy can be collapsed.
    /// </summary>
    public static class KoboldImportOptimize
    {
        const string MalePath   = "Assets/7.Models/kobold-trap-setter-male/source/Kobold Trap-Setter-male.fbx";
        const string FemalePath = "Assets/7.Models/kobold-trap-setter-female/source/Kobold Trap-Setter female.fbx";

        const string Tag = "[KoboldImportOptimize]";

        [MenuItem("Tools/Optimize/Kobold Import")]
        public static void OptimizeKoboldImport()
        {
            ReportAndOptimize(MalePath,   "KoboldMale");
            ReportAndOptimize(FemalePath, "KoboldFemale");
            Debug.Log($"{Tag} Done. Check Console for poly-count report and reimport results.");
        }

        static void ReportAndOptimize(string assetPath, string label)
        {
            // ── Phase 1: poly-count report ────────────────────────────────────────
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (allAssets == null || allAssets.Length == 0)
            {
                Debug.LogWarning($"{Tag} [{label}] No assets found at '{assetPath}'. Skipping.");
                return;
            }

            int totalTris = 0;
            int totalVerts = 0;
            int meshCount = 0;

            foreach (var asset in allAssets)
            {
                if (asset is Mesh mesh)
                {
                    int tris  = mesh.triangles.Length / 3;
                    int verts = mesh.vertexCount;
                    totalTris  += tris;
                    totalVerts += verts;
                    meshCount++;
                    Debug.Log($"{Tag} [{label}] Mesh '{mesh.name}': {tris:N0} tris, {verts:N0} verts");
                }
            }

            if (meshCount == 0)
            {
                Debug.LogWarning($"{Tag} [{label}] No Mesh sub-assets found at '{assetPath}'. " +
                                 "The FBX may not yet be imported, or mesh read/write is disabled at the OS level.");
            }

            Debug.Log($"{Tag} [{label}] TOTAL: {totalTris:N0} triangles, {totalVerts:N0} vertices " +
                      $"across {meshCount} mesh(es). Quest 2 budget guidance: " +
                      $"<=15-20k tris per character (3 on screen = {totalTris * 3:N0} tris combined).");

            // ── Phase 2: apply optimized import settings ──────────────────────────
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"{Tag} [{label}] Could not get ModelImporter for '{assetPath}'. Skipping optimization.");
                return;
            }

            Debug.Log($"{Tag} [{label}] Applying optimized import settings...");

            // Mesh compression & read/write
            importer.meshCompression       = ModelImporterMeshCompression.Medium;
            importer.isReadable            = false;    // already false per meta; explicit for idempotency

            // Mesh topology optimisation (maps to meshOptimizationFlags internally)
            importer.optimizeMeshPolygons  = true;
            importer.optimizeMeshVertices  = true;
            importer.weldVertices          = true;     // already true per meta; explicit for idempotency

            // Strip data unused by static skinned NPCs
            importer.importBlendShapes     = false;    // no facial animation
            importer.importVisibility      = false;
            importer.importCameras         = false;
            importer.importLights          = false;
            importer.importConstraints     = false;

            // Rig: keep Generic so optimizeGameObjects is valid (see header rationale)
            // importer.animationType is intentionally left unchanged (Generic = 2)

            // Collapse the 85-bone CC_Base hierarchy; no sub-bone Animator lookups needed
            importer.optimizeGameObjects        = true;
            importer.extraExposedTransformPaths = System.Array.Empty<string>();

            // Skip the useless T-pose + stray take; no Animator consumes these clips
            importer.importAnimation = false;

            Debug.Log($"{Tag} [{label}] Settings applied. Calling SaveAndReimport()...");
            importer.SaveAndReimport();
            Debug.Log($"{Tag} [{label}] SaveAndReimport() complete.");
        }
    }
}
