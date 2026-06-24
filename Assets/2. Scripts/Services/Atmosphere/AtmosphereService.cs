using Services.GameState;
using Services.UpdateService;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
// This service lives under the `Services` namespace, so the sibling namespace `Services.GameState`
// shadows the `GameState` enum (same simple name) — referencing it bare gives CS0118. Alias the enum.
using GameStateId = Services.GameState.GameState;

namespace Services.Atmosphere
{
    /// <summary>
    /// Atmosphere driver: blends the scene between a neutral "day" look and a warm, dim "night" look
    /// based on game state, and plays a short fade-to-black-and-back "blink" on night start/end.
    ///
    /// Code-only (no MonoBehaviour, no scene wiring): a plain service that ticks off IUpdateService and
    /// lazily resolves the URP Global Volume + bar lights once the Bar scene is loaded. Post-processing
    /// overrides are added to the Volume's runtime profile *instance* (volume.profile, same as
    /// ComfortVignette) so the shared profile asset is never modified.
    ///
    /// Everything is derived from a single smoothed scalar <c>_night</c> (0 = day, 1 = night), which
    /// keeps the blend frame-rate independent and trivially tunable via the constants below.
    /// </summary>
    public sealed class AtmosphereService : IAtmosphereService, IUpdateListener
    {
        // --- Tunables (placeholder values; safe to tweak in code) ---
        private const float DayExposure = 0f;
        private const float NightExposure = -0.6f;
        private const float NightSaturation = -10f;        // ColorAdjustments saturation is -100..100
        private const float DayBloom = 0.4f;
        private const float NightBloom = 1.2f;
        private const float BloomThreshold = 0.9f;
        private const float NightPointLightScale = 0.6f;   // dim the room lights at night
        private const float NightDirLightScale = 0.8f;     // dim the sun only slightly (keep legibility)
        private const float DarkExposure = -8f;            // blink peak (near black)
        private const float BlinkDuration = 0.7f;
        private const float Smoothing = 2.5f;              // higher = snappier day/night blend

        private static readonly Color WarmFilter = new Color(1f, 0.86f, 0.7f, 1f);
        private static readonly Color WarmLight = new Color(1f, 0.82f, 0.6f, 1f);

        private IUpdateService _updates;
        private IGameStateService _state;

        private Volume _volume;
        private Bloom _bloom;
        private ColorAdjustments _grading;
        private bool _resolved;
        private float _resolveCooldown;

        private Light[] _lights;
        private float[] _baseIntensity;
        private Color[] _baseColor;
        private bool[] _isDirectional;

        private float _night;        // smoothed 0..1
        private float _targetNight;  // 0 day, 1 night
        private float _transition;   // 1 → 0 over a blink; 0 = idle

        // Warm-up cover: hold the screen black after a scene load while the Quest CPU/GPU clocks ramp
        // (the game starts ~40 FPS and climbs), then fade in so the ramp isn't seen. Darkens through the
        // same exposure path as the blink; combined via max().
        private float _coverLevel;   // 1 = full black, 0 = clear
        private float _coverHold;    // seconds to stay fully black before fading in
        private float _coverFade;    // seconds to fade from black to clear
        private bool  _covering;

        public void Initialize()
        {
            if (ServiceLocator.TryGet<IUpdateService>(out _updates))
                _updates.AddUpdateListener(this);

            if (ServiceLocator.TryGet<IGameStateService>(out _state))
            {
                _state.StateChanged += OnStateChanged;
                _targetNight = IsNight(_state.Current) ? 1f : 0f;
                _night = _targetNight;
            }
        }

        private static bool IsNight(GameStateId s) => s == GameStateId.NightRunning;

        private void OnStateChanged(GameStateId from, GameStateId to)
        {
            _targetNight = IsNight(to) ? 1f : 0f;

            // Blink on the meaningful boundaries: lights coming up for the night, and night wrapping up.
            if (to == GameStateId.NightRunning || to == GameStateId.NightSummary)
                _transition = 1f;
        }

        /// <summary>
        /// Hold the screen black for <paramref name="holdSeconds"/> — e.g. right after the gameplay scene
        /// becomes active, while the Quest clocks ramp from the cold-start low — then fade in over
        /// <paramref name="fadeSeconds"/>. Reuses the blink's exposure path. Forces an immediate Volume
        /// re-bind so the black applies to the new scene this tick instead of waiting the resolve cooldown.
        /// </summary>
        public void CoverFadeIn(float holdSeconds, float fadeSeconds)
        {
            _coverLevel = 1f;
            _coverHold = Mathf.Max(0f, holdSeconds);
            _coverFade = Mathf.Max(0.01f, fadeSeconds);
            _covering = true;
            _resolved = false;       // rebind to the new scene's Volume now…
            _resolveCooldown = 0f;   // …without waiting the 1s throttle (ramp must not be visible)
        }

        public void MyUpdate()
        {
            float dt = Time.deltaTime;

            // Lazily bind to the scene's Volume + lights once the Bar scene is up; rebind if the scene
            // changed (cached references become Unity-null). Throttle attempts so a profile-less scene
            // doesn't FindAnyObjectByType every frame.
            if (_volume == null) _resolved = false;
            if (!_resolved)
            {
                _resolveCooldown -= dt;
                if (_resolveCooldown > 0f) return;
                _resolveCooldown = 1f;
                Resolve();
                if (_volume == null && (_lights == null || _lights.Length == 0)) return;
                _resolved = true;
            }

            // Frame-rate-independent smoothing toward the target day/night blend.
            float k = 1f - Mathf.Exp(-Smoothing * dt);
            _night = Mathf.Lerp(_night, _targetNight, k);

            // Blink envelope: sin() so it eases to black at the midpoint and back (no hard jump).
            if (_transition > 0f)
            {
                _transition -= dt / BlinkDuration;
                if (_transition < 0f) _transition = 0f;
            }
            float blink = _transition > 0f ? Mathf.Sin(_transition * Mathf.PI) : 0f;

            // Warm-up cover: stay fully black for the hold, then ramp the cover down to clear.
            if (_covering)
            {
                if (_coverHold > 0f) _coverHold -= dt;
                else
                {
                    _coverLevel -= dt / _coverFade;
                    if (_coverLevel <= 0f) { _coverLevel = 0f; _covering = false; }
                }
            }

            // Blink and cover both darken through the same exposure; the stronger one wins.
            ApplyGrading(Mathf.Max(blink, _coverLevel));
            ApplyLights();
        }

        private void ApplyGrading(float dark)
        {
            if (_grading != null)
            {
                float exposure = Mathf.Lerp(DayExposure, NightExposure, _night);
                exposure = Mathf.Lerp(exposure, DarkExposure, dark);
                _grading.postExposure.value = exposure;
                _grading.colorFilter.value = Color.Lerp(Color.white, WarmFilter, _night);
                _grading.saturation.value = Mathf.Lerp(0f, NightSaturation, _night);
            }

            if (_bloom != null)
                _bloom.intensity.value = Mathf.Lerp(DayBloom, NightBloom, _night);
        }

        private void ApplyLights()
        {
            if (_lights == null) return;
            for (int i = 0; i < _lights.Length; i++)
            {
                var l = _lights[i];
                if (l == null) continue;
                float nightScale = _isDirectional[i] ? NightDirLightScale : NightPointLightScale;
                l.intensity = _baseIntensity[i] * Mathf.Lerp(1f, nightScale, _night);
                // Nudge toward warm at night without fully overriding the authored colour.
                l.color = Color.Lerp(_baseColor[i], WarmLight, _night * 0.5f);
            }
        }

        private void Resolve()
        {
            _volume = Object.FindAnyObjectByType<Volume>();
            if (_volume != null && _volume.profile != null)
            {
                var profile = _volume.profile; // instance (clones sharedProfile) — never touches the asset
                EnsureBloom(profile);
                EnsureGrading(profile);
            }

            ResolveLights();
        }

        private void EnsureBloom(VolumeProfile profile)
        {
            if (!profile.TryGet(out _bloom)) _bloom = profile.Add<Bloom>(true);
            _bloom.active = true;
            _bloom.intensity.overrideState = true;
            _bloom.threshold.overrideState = true;
            _bloom.threshold.value = BloomThreshold;
        }

        private void EnsureGrading(VolumeProfile profile)
        {
            if (!profile.TryGet(out _grading)) _grading = profile.Add<ColorAdjustments>(true);
            _grading.active = true;
            _grading.postExposure.overrideState = true;
            _grading.colorFilter.overrideState = true;
            _grading.saturation.overrideState = true;
        }

        private void ResolveLights()
        {
            Light[] found;
            var root = GameObject.Find("RoomLights");
            if (root != null)
                found = root.GetComponentsInChildren<Light>(true);
            else
                found = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);

            if (found == null || found.Length == 0)
            {
                _lights = null;
                return;
            }

            _lights = found;
            _baseIntensity = new float[found.Length];
            _baseColor = new Color[found.Length];
            _isDirectional = new bool[found.Length];
            for (int i = 0; i < found.Length; i++)
            {
                _baseIntensity[i] = found[i].intensity;
                _baseColor[i] = found[i].color;
                _isDirectional[i] = found[i].type == LightType.Directional;
            }
        }
    }
}
