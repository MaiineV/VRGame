using Data.Enums;
using Services;
using Services.Audio;
using Services.GameState;
using UnityEngine;

namespace UI.Diegetic
{
    public sealed class MusicController : MonoBehaviour
    {
        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float _idleVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _nightVolume = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _feedbackVolume = 0.6f;
        [Tooltip("Steady bar room-tone loop, runs the whole time the player is in the bar.")]
        [SerializeField, Range(0f, 1f)] private float _ambienceVolume = 0.3f;

        private IAudioService _audio;
        private IGameStateService _state;
        private int _musicHandle;
        private int _ambienceHandle;

        void OnEnable()
        {
            if (!ServiceLocator.TryGet<IAudioService>(out _audio)) return;
            if (!ServiceLocator.TryGet<IGameStateService>(out _state)) return;

            _state.StateChanged += OnStateChanged;
            ApplyMusic(_state.Current);

            // Persistent bar ambience, independent of the music track (separate pool slot).
            _ambienceHandle = _audio.StartLoop(SfxId.BarAmbience, null, Vector3.zero, _ambienceVolume);
        }

        void OnDisable()
        {
            if (_state != null) _state.StateChanged -= OnStateChanged;
            StopCurrentMusic();
            if (_ambienceHandle != 0 && _audio != null)
            {
                _audio.StopLoop(_ambienceHandle);
                _ambienceHandle = 0;
            }
            _audio = null;
            _state = null;
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            if (_audio == null) return;

            if (from == GameState.Idle && to == GameState.NightRunning)
                _audio.PlayOneShot2D(SfxId.NightStart, _feedbackVolume);
            else if (from == GameState.NightRunning && to == GameState.NightSummary)
                _audio.PlayOneShot2D(SfxId.NightEnd, _feedbackVolume);

            ApplyMusic(to);
        }

        private void ApplyMusic(GameState current)
        {
            StopCurrentMusic();
            if (_audio == null) return;

            switch (current)
            {
                case GameState.Idle:
                    _musicHandle = _audio.StartLoop(SfxId.MusicIdle, null, Vector3.zero, _idleVolume);
                    break;
                case GameState.NightRunning:
                    _musicHandle = _audio.StartLoop(SfxId.MusicNight, null, Vector3.zero, _nightVolume);
                    break;
            }
        }

        private void StopCurrentMusic()
        {
            if (_musicHandle != 0 && _audio != null)
            {
                _audio.StopLoop(_musicHandle);
                _musicHandle = 0;
            }
        }
    }
}
