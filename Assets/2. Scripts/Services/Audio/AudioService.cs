using Data.Enums;
using Data.SO;
using UnityEngine;
using Utilities;

namespace Services.Audio
{
    /// <summary>
    /// Preallocated AudioSource pool, no runtime Instantiate. One-shots rotate
    /// through free slots; loops reserve a slot tagged with a generation counter
    /// so stale handles from a later rotation can't stop the wrong source.
    /// </summary>
    public sealed class AudioService : IAudioService
    {
        private const string DatabaseResourcePath = "Database/SfxDatabase";
        private const int PoolSize = 12;

        private SfxDatabase _db;
        private AudioSource[] _sources;
        private ushort[] _generations;
        private bool[] _looping;
        private float[] _lastPlayTime;   // per-SfxId, indexed by (byte)id
        private int _rrCursor;
        private Transform _root;

        public void Initialize()
        {
            _db = Resources.Load<SfxDatabase>(DatabaseResourcePath);
            if (_db == null)
            {
                MyLogger.LogWarning($"[AudioService] No SfxDatabase found at Resources/{DatabaseResourcePath}. SFX disabled.");
            }

            var host = new GameObject("[AudioService]");
            Object.DontDestroyOnLoad(host);
            _root = host.transform;

            _sources = new AudioSource[PoolSize];
            _generations = new ushort[PoolSize];
            _looping = new bool[PoolSize];
            _lastPlayTime = new float[256];

            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Src_" + i);
                go.transform.SetParent(_root, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.dopplerLevel = 0f;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.minDistance = 1f;
                src.maxDistance = 15f;
                _sources[i] = src;
                _generations[i] = 1;
            }
        }

        public void PlayOneShot(SfxId id, Vector3 position, float volumeScale = 1f)
        {
            if (!TryReserveForOneShot(id, out var src, out var entry, out float vol, volumeScale)) return;
            src.transform.position = position;
            ConfigureSource(src, entry, vol, loop: false, attach: null);
            src.Play();
        }

        public void PlayOneShot2D(SfxId id, float volumeScale = 1f)
        {
            if (!TryReserveForOneShot(id, out var src, out var entry, out float vol, volumeScale)) return;
            src.transform.localPosition = Vector3.zero;
            ConfigureSource(src, entry, vol, loop: false, attach: null);
            src.spatialBlend = 0f;
            src.Play();
        }

        public int StartLoop(SfxId id, Transform attachTo, Vector3 fallbackWorldPos, float volumeScale = 1f)
        {
            if (_db == null || !_db.TryGet(id, out var entry)) return 0;
            int idx = FindFreeSlot();
            if (idx < 0) return 0;

            var src = _sources[idx];
            if (attachTo != null)
            {
                src.transform.SetParent(attachTo, false);
                src.transform.localPosition = Vector3.zero;
            }
            else
            {
                src.transform.SetParent(_root, false);
                src.transform.position = fallbackWorldPos;
            }

            float vol = entry.volume * volumeScale;
            ConfigureSource(src, entry, vol, loop: true, attach: attachTo);
            _looping[idx] = true;
            src.Play();

            return PackHandle(idx, _generations[idx]);
        }

        public void StopLoop(int handle)
        {
            if (!TryResolveHandle(handle, out int idx)) return;
            var src = _sources[idx];
            src.Stop();
            src.clip = null;
            src.loop = false;
            src.transform.SetParent(_root, false);
            _looping[idx] = false;
            unchecked { _generations[idx]++; if (_generations[idx] == 0) _generations[idx] = 1; }
        }

        public void SetLoopVolume(int handle, float volumeScale)
        {
            if (!TryResolveHandle(handle, out int idx)) return;
            if (_db == null) return;
            // Rescale from the clip's configured base volume if we can infer it — otherwise
            // treat volumeScale as absolute. Keeping it absolute avoids a reverse lookup.
            _sources[idx].volume = Mathf.Clamp01(volumeScale);
        }

        private bool TryReserveForOneShot(SfxId id, out AudioSource src, out SfxDatabase.Entry entry, out float volume, float volumeScale)
        {
            src = null; entry = default; volume = 0f;
            if (_db == null || !_db.TryGet(id, out entry)) return false;

            if (entry.minRetriggerInterval > 0f)
            {
                float now = Time.unscaledTime;
                int key = (byte)id;
                if (now - _lastPlayTime[key] < entry.minRetriggerInterval) return false;
                _lastPlayTime[key] = now;
            }

            int idx = FindFreeSlot();
            if (idx < 0) return false;
            src = _sources[idx];
            volume = entry.volume * volumeScale;
            return true;
        }

        private int FindFreeSlot()
        {
            // Prefer a non-looping source that has finished playing. Round-robin cursor.
            for (int step = 0; step < PoolSize; step++)
            {
                int idx = (_rrCursor + step) % PoolSize;
                if (_looping[idx]) continue;
                if (_sources[idx].isPlaying) continue;
                _rrCursor = (idx + 1) % PoolSize;
                return idx;
            }
            // Last resort: steal the oldest non-looping source.
            for (int step = 0; step < PoolSize; step++)
            {
                int idx = (_rrCursor + step) % PoolSize;
                if (_looping[idx]) continue;
                _sources[idx].Stop();
                _rrCursor = (idx + 1) % PoolSize;
                return idx;
            }
            return -1;
        }

        private static void ConfigureSource(AudioSource src, SfxDatabase.Entry entry, float volume, bool loop, Transform attach)
        {
            src.clip = entry.clip;
            src.volume = Mathf.Clamp01(volume);
            src.spatialBlend = entry.spatialBlend;
            src.loop = loop;
            if (!loop)
            {
                float pMin = entry.pitchMin <= 0f ? 1f : entry.pitchMin;
                float pMax = entry.pitchMax <= 0f ? pMin : entry.pitchMax;
                src.pitch = pMin >= pMax ? pMin : Random.Range(pMin, pMax);
            }
            else
            {
                src.pitch = entry.pitchMin <= 0f ? 1f : entry.pitchMin;
            }
        }

        private static int PackHandle(int idx, ushort gen) => ((int)gen << 16) | (idx & 0xFFFF);

        private bool TryResolveHandle(int handle, out int idx)
        {
            idx = handle & 0xFFFF;
            if (handle == 0 || idx < 0 || idx >= PoolSize) { idx = -1; return false; }
            ushort gen = (ushort)((handle >> 16) & 0xFFFF);
            if (_generations[idx] != gen) { idx = -1; return false; }
            return true;
        }
    }
}
