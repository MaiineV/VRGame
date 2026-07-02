using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEditor;
using UnityEngine;
using Utilities;

namespace EditorTools
{
    /// <summary>
    /// Drives the migration from the custom OVR interaction scripts (SimpleVRGrabber /
    /// ThumbstickLocomotion) to the Meta Interaction SDK, invoking the SDK's own QuickActions
    /// wizards (the same ones the course docs use via right-click ▸ Interaction SDK) through
    /// reflection, since they are internal. Steps are separate menu items so each can be run and
    /// verified incrementally.
    /// </summary>
    public static class SdkMigration
    {
        private const string WizardsAsm = "Oculus.Interaction.Editor";
        private const string WizardsNs = "Oculus.Interaction.Editor.QuickActions";

        // ------------------------------------------------------------------ step 1: rig interactors

        [MenuItem("Pour Decisions/SDK Migration/1 Add Interactors To Hands")]
        public static void AddInteractorsToHands()
        {
            var hands = UnityEngine.Object.FindObjectsByType<Hand>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(h => h.GetComponent<Controller>() != null)   // the controller-driven hand roots
                .ToArray();
            if (hands.Length == 0) { MyLogger.LogWarning("[SdkMigration] No controller-driven hands found."); return; }

            foreach (var hand in hands)
            {
                var created = RunWizard("ControllerHandInteractorWizard", hand.gameObject);
                MyLogger.LogInfo($"[SdkMigration] {hand.name}: added {created.Count} interactor group(s): "
                                 + string.Join(", ", created.Select(g => g.name)));
            }
            MarkActiveSceneDirty();
        }

        // ------------------------------------------------------------------ step 2: grabbable prefabs

        private static readonly string[] GrabbablePrefabs =
        {
            "Assets/4. Prefabs/Bottles/Bottle_Champagne.prefab",
            "Assets/4. Prefabs/Bottles/Bottle_Hennessy.prefab",
            "Assets/4. Prefabs/Bottles/Bottle_JackDaniel.prefab",
            "Assets/4. Prefabs/Bottles/Bottle_SimpleBottle.prefab",
            "Assets/4. Prefabs/Bottles/Bottle_Wine.prefab",
            "Assets/4. Prefabs/Glass.prefab",
        };

        [MenuItem("Pour Decisions/SDK Migration/2 Make Prefabs Grabbable")]
        public static void MakePrefabsGrabbable()
        {
            foreach (var path in GrabbablePrefabs)
            {
                var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
                try
                {
                    // The wizards skip work that's already present, so re-running is safe.
                    RunWizard("GrabWizard", root);
                    RunWizard("DistanceGrabWizard", root);

                    if (root.GetComponent<Gameplay.Interactions.GrabBridgeAdapter>() == null)
                        root.AddComponent<Gameplay.Interactions.GrabBridgeAdapter>();
                    bool isBottle = root.GetComponent<Gameplay.Interactions.Bottle>() != null;
                    if (isBottle && root.GetComponent<Gameplay.Interactions.GrabGateEnforcer>() == null)
                        root.AddComponent<Gameplay.Interactions.GrabGateEnforcer>();

                    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
                    MyLogger.LogInfo($"[SdkMigration] Grabbable ok: {path}");
                }
                finally
                {
                    UnityEditor.PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        // ------------------------------------------------------------------ step 3: remove legacy

        [MenuItem("Pour Decisions/SDK Migration/3 Remove Legacy Interaction")]
        public static void RemoveLegacyInteraction()
        {
            int removed = 0;
            foreach (var g in UnityEngine.Object.FindObjectsByType<Gameplay.Interactions.SimpleVRGrabber>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                UnityEngine.Object.DestroyImmediate(g);
                removed++;
            }
            foreach (var l in UnityEngine.Object.FindObjectsByType<Gameplay.Interactions.ThumbstickLocomotion>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var rigGo = l.gameObject;
                UnityEngine.Object.DestroyImmediate(l);
                removed++;
                if (rigGo.GetComponent<Gameplay.Interactions.HeightCalibrator>() == null)
                    rigGo.AddComponent<Gameplay.Interactions.HeightCalibrator>();
            }
            MyLogger.LogInfo($"[SdkMigration] Removed {removed} legacy component(s); HeightCalibrator ensured.");
            MarkActiveSceneDirty();
        }

        // ------------------------------------------------------------------ step 4: poke buttons

        [MenuItem("Pour Decisions/SDK Migration/4 Migrate Poke Buttons")]
        public static void MigratePokeButtons()
        {
            foreach (var button in UnityEngine.Object.FindObjectsByType<UI.Diegetic.PokeButton>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (button.GetComponentInChildren<PokeInteractable>(true) == null)
                    RunWizard("PokeWizard", button.gameObject);

                var poke = button.GetComponentInChildren<PokeInteractable>(true);
                if (poke != null)
                {
                    // Fit the template's poke surface to this button's own collider footprint.
                    var col = button.GetComponent<Collider>();
                    var b = col.bounds;
                    var surfaceRoot = poke.transform;
                    surfaceRoot.position = new Vector3(b.center.x, b.max.y, b.center.z);
                    surfaceRoot.rotation = Quaternion.LookRotation(Vector3.up, button.transform.forward);
                }

                if (button.GetComponent<UI.Diegetic.PokeButtonSdkAdapter>() == null)
                    button.gameObject.AddComponent<UI.Diegetic.PokeButtonSdkAdapter>();
                MyLogger.LogInfo($"[SdkMigration] Poke button migrated: {button.name}");
            }

            // The physical finger colliders are superseded by the SDK PokeInteractor.
            foreach (var rigRoot in UnityEngine.Object.FindObjectsByType<OVRCameraRig>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var fingers = rigRoot.GetComponentsInChildren<Transform>(true)
                    .Where(t => t != null && t.name.StartsWith("PokeFinger"))
                    .Select(t => t.gameObject).ToArray();
                foreach (var f in fingers) UnityEngine.Object.DestroyImmediate(f);
                if (fingers.Length > 0)
                    MyLogger.LogInfo($"[SdkMigration] Removed {fingers.Length} legacy PokeFinger object(s).");
            }
            MarkActiveSceneDirty();
        }

        // ------------------------------------------------------------------ step 5: main menu ray

        [MenuItem("Pour Decisions/SDK Migration/5 Migrate MainMenu Ray")]
        public static void MigrateMainMenuRay()
        {
            var asm = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == WizardsAsm);
            var utils = asm.GetType($"{WizardsNs}.InteractorUtils", throwOnError: true);
            var typesEnum = asm.GetType($"{WizardsNs}.InteractorTypes", throwOnError: true);
            object rayFlag = Enum.ToObject(typesEnum, 4); // InteractorTypes.Ray
            var add = utils.GetMethod("AddInteractorsToControllerHand",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var hmd = UnityEngine.Object.FindAnyObjectByType<Hmd>(FindObjectsInactive.Include);

            foreach (var hand in UnityEngine.Object.FindObjectsByType<Hand>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var controller = hand.GetComponent<Controller>();
                if (controller == null) continue;   // only the per-side interaction roots
                var holder = hand.transform.Find("Interactors");
                if (holder == null) continue;
                var group = holder.GetComponent<InteractorGroup>();
                var created = (IEnumerable<GameObject>)add.Invoke(null,
                    new object[] { rayFlag, controller, hand, hmd, holder, group });
                MyLogger.LogInfo($"[SdkMigration] {hand.name}: ray interactor(s) added: "
                                 + string.Join(", ", created.Select(g => g.name)));
            }

            foreach (var laser in UnityEngine.Object.FindObjectsByType<UI.Menu.VrLaserPointer>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var line = laser.GetComponent<LineRenderer>();
                UnityEngine.Object.DestroyImmediate(laser);
                if (line != null) UnityEngine.Object.DestroyImmediate(line);
                MyLogger.LogInfo("[SdkMigration] Removed legacy VrLaserPointer.");
            }
            MarkActiveSceneDirty();
        }

        // ------------------------------------------------------------------ wizard plumbing

        /// <summary>Runs an internal QuickActions wizard on a target with its default settings and
        /// returns the GameObjects it created.</summary>
        public static List<GameObject> RunWizard(string wizardTypeName, GameObject target)
        {
            var asm = AppDomain.CurrentDomain.GetAssemblies()
                .First(a => a.GetName().Name == WizardsAsm);
            var wizardType = asm.GetType($"{WizardsNs}.{wizardTypeName}", throwOnError: true);
            var baseType = asm.GetType($"{WizardsNs}.QuickActionsWizard", throwOnError: true);
            var method = baseType
                .GetMethod("CreateWithDefaults", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .MakeGenericMethod(wizardType);
            var result = method.Invoke(null, new object[] { target, true, null });
            return ((IEnumerable<GameObject>)result).ToList();
        }

        private static void MarkActiveSceneDirty()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }
    }
}
