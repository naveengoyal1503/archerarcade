using System.Collections.Generic;
using UnityEngine;

namespace ArcherArcade.Feel
{
    /// <summary>
    /// Sound effects and music (GAME_DESIGN §11): a pool of SFX voices, 2–3 variations per id with ±4 % pitch
    /// jitter, looping sounds (bow creak, burning), music with a 0.6 s crossfade, stingers and ducking (pause,
    /// knockout slow-mo). Clips load by id from Resources/Audio/Final/ (real sounds) or Audio/Generated/
    /// (Tools/gen_sounds.py placeholders); variations are named &lt;id&gt;_v1, _v2, _v3.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        const int Voices = 16;
        const float Crossfade = 0.6f;
        const float PitchJitter = 0.04f;

        readonly Dictionary<string, AudioClip[]> _cache = new Dictionary<string, AudioClip[]>();
        static readonly AudioClip[] None = new AudioClip[0];
        AudioSource[] _voices;
        int _nextVoice;
        AudioSource _musicA, _musicB, _sting;
        AudioSource _creak, _fire;
        bool _aIsCurrent = true;
        string _musicId;
        float _fade = 1f;
        float _musicVolume = 0.7f, _sfxVolume = 0.8f;
        float _duck = 1f, _duckTarget = 1f;
        uint _rng = 0x9E3779B9u;

        void Awake()
        {
            _voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++) _voices[i] = NewSource("sfx" + i, false);
            _musicA = NewSource("musicA", true);
            _musicB = NewSource("musicB", true);
            _sting = NewSource("sting", false);
            _creak = NewSource("creak", true);
            _fire = NewSource("fire", true);
        }

        AudioSource NewSource(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = 0f;
            return s;
        }

        public void SetVolumes(int music, int sfx)
        {
            _musicVolume = Mathf.Clamp01(music / 100f);
            _sfxVolume = Mathf.Clamp01(sfx / 100f);
            ApplyMusicVolume();
            _creak.volume = _sfxVolume * 0.5f;
            _fire.volume = _sfxVolume * 0.4f;
        }

        public bool SfxOn => _sfxVolume > 0f;

        AudioClip[] Clips(string id)
        {
            if (_cache.TryGetValue(id, out AudioClip[] clips)) return clips;
            clips = LoadVariations("Audio/Final/" + id);
            if (clips.Length == 0) clips = LoadVariations("Audio/Generated/" + id);
            _cache[id] = clips;
            return clips;
        }

        static AudioClip[] LoadVariations(string path)
        {
            var list = new List<AudioClip>(3);
            for (int v = 1; v <= 3; v++)
            {
                var c = Resources.Load<AudioClip>(path + "_v" + v);
                if (c) list.Add(c);
            }
            if (list.Count == 0)
            {
                var single = Resources.Load<AudioClip>(path);
                if (single) list.Add(single);
            }
            return list.Count == 0 ? None : list.ToArray();
        }

        float Random01()
        {
            _rng ^= _rng << 13;
            _rng ^= _rng >> 17;
            _rng ^= _rng << 5;
            return (_rng & 0xFFFFFF) / 16777216f;
        }

        /// <summary>Plays a one-shot SFX (visual feedback randomness only: never affects gameplay).</summary>
        public void Play(string id, float volume = 1f, float pitch = 1f)
        {
            if (_sfxVolume <= 0f) return;
            AudioClip[] clips = Clips(id);
            if (clips.Length == 0) return;
            AudioClip clip = clips[(int)(Random01() * clips.Length) % clips.Length];
            AudioSource v = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % Voices;
            v.clip = clip;
            v.volume = _sfxVolume * volume;
            v.pitch = pitch * (1f + (Random01() * 2f - 1f) * PitchJitter);
            v.Play();
        }

        /// <summary>Bow creak loop while drawing; pitch rises with power (GAME_DESIGN §3.1).</summary>
        public void Creak(bool on, float power = 0f)
        {
            SetLoop(_creak, SoundId.BowCreakLoop, on, 0.5f);
            if (on) _creak.pitch = 0.85f + power * 0.5f;
        }

        /// <summary>Crackle loop while an archer is on fire.</summary>
        public void Burning(bool on) => SetLoop(_fire, SoundId.OnFireLoop, on, 0.4f);

        void SetLoop(AudioSource src, string id, bool on, float volume)
        {
            if (!on || _sfxVolume <= 0f)
            {
                if (src.isPlaying) src.Stop();
                return;
            }
            if (src.isPlaying) return;
            AudioClip[] clips = Clips(id);
            if (clips.Length == 0) return;
            src.clip = clips[0];
            src.volume = _sfxVolume * volume;
            src.Play();
        }

        public void PlayMusic(string id)
        {
            if (id == _musicId) return;
            _musicId = id;
            AudioClip[] clips = Clips(id);
            AudioSource next = _aIsCurrent ? _musicB : _musicA;
            _aIsCurrent = !_aIsCurrent;
            next.clip = clips.Length > 0 ? clips[0] : null;
            if (next.clip) next.Play();
            _fade = 0f;
        }

        public void StopMusic()
        {
            _musicId = null;
            _musicA.Stop();
            _musicB.Stop();
        }

        /// <summary>Victory / defeat stinger over ducked music.</summary>
        public void Sting(string id)
        {
            AudioClip[] clips = Clips(id);
            if (clips.Length == 0) return;
            _sting.clip = clips[0];
            _sting.volume = Mathf.Max(_musicVolume, _sfxVolume * 0.8f);
            _sting.Play();
            _duck = 0.25f;
        }

        /// <summary>Music down (pause, knockout slow-mo) or back up.</summary>
        public void Duck(bool on) => _duckTarget = on ? 0.35f : 1f;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_fade < 1f) _fade = Mathf.Min(1f, _fade + dt / Crossfade);
            float target = _sting.isPlaying ? 0.25f : _duckTarget;
            _duck = Mathf.MoveTowards(_duck, target, dt * 1.5f);
            ApplyMusicVolume();
        }

        void ApplyMusicVolume()
        {
            if (_musicA == null) return;
            AudioSource cur = _aIsCurrent ? _musicA : _musicB;
            AudioSource old = _aIsCurrent ? _musicB : _musicA;
            cur.volume = _musicVolume * _duck * _fade;
            old.volume = _musicVolume * _duck * (1f - _fade);
            if (_fade >= 1f && old.isPlaying) old.Stop();
        }
    }
}
