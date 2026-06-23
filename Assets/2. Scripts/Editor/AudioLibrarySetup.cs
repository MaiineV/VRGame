#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Data.Enums;
using Data.SO;
using Services;
using Services.Audio;
using UnityEditor;
using UnityEngine;

namespace PourDecisions.EditorTools
{
    /// <summary>
    /// One-click (re)build of the runtime SfxDatabase from the natural CC0/PD clips in
    /// <c>Assets/6. Audio</c>. Each SfxId is matched to the AudioClip whose file name (without
    /// extension) equals the SfxId name, audio import settings are applied (mono + compressed for 3D
    /// SFX, streaming stereo for music/ambience) and per-entry mix params are written. This decouples
    /// the database from clip GUIDs/extensions: to swap a sound, drop a file with the matching base
    /// name into the folder and re-run "Rebuild SFX Database". See Assets/6. Audio/ATTRIBUTIONS.md.
    /// </summary>
    public static class AudioLibrarySetup
    {
        // The runtime AudioService loads "Database/SfxDatabase" from Resources, so this copy is authoritative.
        private const string DbPath = "Assets/Resources/Database/SfxDatabase.asset";
        private const string AudioFolder = "Assets/6. Audio";

        private struct Cfg
        {
            public SfxId id;
            public float vol, pMin, pMax, spatial, retrigger;
            public bool loop, streaming; // streaming => stereo music/ambience; otherwise mono 3D SFX
            public Cfg(SfxId id, float vol, float pMin, float pMax, float spatial, float retrigger, bool loop, bool streaming)
            { this.id = id; this.vol = vol; this.pMin = pMin; this.pMax = pMax; this.spatial = spatial; this.retrigger = retrigger; this.loop = loop; this.streaming = streaming; }
        }

        // id, volume, pitchMin, pitchMax, spatialBlend, minRetriggerInterval(s), loop, streaming
        private static readonly Cfg[] Table =
        {
            new Cfg(SfxId.PourLoop,       0.80f, 1.00f, 1.00f, 1f, 0.00f, true,  true),
            new Cfg(SfxId.GlassFull,      0.70f, 0.98f, 1.05f, 1f, 1.00f, false, false),
            new Cfg(SfxId.GlassBreak,     0.90f, 0.95f, 1.05f, 1f, 0.05f, false, false),
            new Cfg(SfxId.BottleBreak,    0.90f, 0.90f, 1.00f, 1f, 0.05f, false, false),
            new Cfg(SfxId.GlassPlace,     0.80f, 0.95f, 1.10f, 1f, 0.06f, false, false),
            new Cfg(SfxId.BottlePlace,    0.85f, 0.90f, 1.05f, 1f, 0.06f, false, false),
            new Cfg(SfxId.CashSale,       0.80f, 1.00f, 1.00f, 1f, 0.00f, false, false),
            new Cfg(SfxId.CashExpense,    0.80f, 1.00f, 1.00f, 1f, 0.00f, false, false),
            new Cfg(SfxId.CustomerServed, 0.80f, 1.00f, 1.00f, 1f, 0.00f, false, false),
            new Cfg(SfxId.CustomerLeft,   0.80f, 1.00f, 1.00f, 1f, 0.00f, false, false),
            new Cfg(SfxId.DrinkSip,       0.90f, 0.95f, 1.08f, 1f, 0.10f, false, false),
            new Cfg(SfxId.NightStart,     0.70f, 1.00f, 1.00f, 0f, 0.00f, false, false),
            new Cfg(SfxId.NightEnd,       0.70f, 1.00f, 1.00f, 0f, 0.00f, false, false),
            new Cfg(SfxId.ButtonPress,    0.60f, 1.00f, 1.00f, 0f, 0.00f, false, false),
            new Cfg(SfxId.Footstep,       0.50f, 0.90f, 1.10f, 1f, 0.00f, false, false),
            new Cfg(SfxId.GrabObject,     0.55f, 0.97f, 1.06f, 1f, 0.05f, false, false),
            new Cfg(SfxId.ReleaseObject,  0.55f, 0.97f, 1.06f, 1f, 0.05f, false, false),
            new Cfg(SfxId.BarAmbience,    0.50f, 1.00f, 1.00f, 0f, 0.00f, true,  true),
            new Cfg(SfxId.MusicIdle,      0.50f, 1.00f, 1.00f, 0f, 0.00f, true,  true),
            new Cfg(SfxId.MusicNight,     0.40f, 1.00f, 1.00f, 0f, 0.00f, true,  true),
        };

        [MenuItem("Pour Decisions/Audio/Rebuild SFX Database")]
        public static void RebuildSfxDatabase()
        {
            var db = AssetDatabase.LoadAssetAtPath<SfxDatabase>(DbPath);
            if (db == null)
            {
                Debug.LogError($"[AudioLibrarySetup] SfxDatabase not found at {DbPath}.");
                return;
            }

            // base name (no extension) -> asset path, for every AudioClip in the audio folder.
            var clipByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                clipByName[Path.GetFileNameWithoutExtension(path)] = path;
            }

            var so = new SerializedObject(db);
            var prop = so.FindProperty("_entries");
            prop.arraySize = Table.Length;

            int resolved = 0;
            var report = new StringBuilder();
            for (int i = 0; i < Table.Length; i++)
            {
                var cfg = Table[i];
                var e = prop.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").enumValueIndex = EnumIndex(cfg.id);
                e.FindPropertyRelative("volume").floatValue = cfg.vol;
                e.FindPropertyRelative("pitchMin").floatValue = cfg.pMin;
                e.FindPropertyRelative("pitchMax").floatValue = cfg.pMax;
                e.FindPropertyRelative("spatialBlend").floatValue = cfg.spatial;
                e.FindPropertyRelative("minRetriggerInterval").floatValue = cfg.retrigger;
                e.FindPropertyRelative("loop").boolValue = cfg.loop;

                if (clipByName.TryGetValue(cfg.id.ToString(), out var clipPath))
                {
                    ApplyImportSettings(clipPath, cfg);
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                    e.FindPropertyRelative("clip").objectReferenceValue = clip;
                    if (clip != null) { resolved++; report.AppendLine($"  OK  {cfg.id,-15} -> {Path.GetFileName(clipPath)}"); }
                    else report.AppendLine($"  ERR {cfg.id,-15} -> failed to load {clipPath}");
                }
                else
                {
                    e.FindPropertyRelative("clip").objectReferenceValue = null;
                    report.AppendLine($"  ERR {cfg.id,-15} -> no file '{cfg.id}.*' in {AudioFolder}");
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();

            Debug.Log($"[AudioLibrarySetup] Rebuilt SfxDatabase ({DbPath}) — resolved {resolved}/{Table.Length} clips:\n{report}");
        }

        [MenuItem("Pour Decisions/Audio/Play All SFX (enter Play mode first)")]
        public static void PlayAllSfx()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[AudioLibrarySetup] Enter Play mode first — Play All SFX drives the runtime AudioService.");
                return;
            }
            if (!ServiceLocator.TryGet<IAudioService>(out var audio))
            {
                Debug.LogWarning("[AudioLibrarySetup] IAudioService not registered yet.");
                return;
            }
            foreach (var cfg in Table)
                audio.PlayOneShot2D(cfg.id, 0.7f);
            Debug.Log($"[AudioLibrarySetup] Play All SFX: triggered {Table.Length} ids via AudioService " +
                      "(overlapping — the 12-source pool steals; a clip that fails to resolve is silently skipped).");
        }

        private static void ApplyImportSettings(string path, Cfg cfg)
        {
            if (!(AssetImporter.GetAtPath(path) is AudioImporter importer)) return;

            var s = importer.defaultSampleSettings;
            var wantLoad = cfg.streaming ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
            bool mono = !cfg.streaming;

            bool changed = false;
            if (s.loadType != wantLoad || s.compressionFormat != AudioCompressionFormat.Vorbis)
            {
                s.loadType = wantLoad;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                importer.defaultSampleSettings = s;
                changed = true;
            }
            if (importer.forceToMono != mono) { importer.forceToMono = mono; changed = true; }
            if (importer.loadInBackground != cfg.streaming) { importer.loadInBackground = cfg.streaming; changed = true; }
            if (changed) importer.SaveAndReimport();
        }

        private static int EnumIndex(SfxId value)
        {
            var values = (SfxId[])Enum.GetValues(typeof(SfxId));
            for (int i = 0; i < values.Length; i++)
                if (values[i] == value) return i;
            return 0;
        }
    }
}
#endif
