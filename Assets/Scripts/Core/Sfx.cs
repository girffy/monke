using UnityEngine;

namespace GorillaSurvivors.Core
{
    // Central place to play the game's (procedurally generated) sound
    // effects. Clips are generated once and cached; playback uses
    // AudioSource.PlayClipAtPoint, which spins up (and auto-destroys) its own
    // temporary GameObject, so nothing here needs manual source management.
    // Requires an AudioListener in the scene (added to the main camera).
    public static class Sfx
    {
        static AudioClip _slam, _roar, _charge, _dash;
        static AudioClip _hurt, _gameOver;
        static AudioClip _enemyHit, _enemyDeath, _throw;
        static AudioClip _xpPickup, _powerupPickup, _levelUp;
        static AudioClip _roundClear, _roundStart;
        static AudioClip _rockExplosion;
        static bool _initialized;

        // A small pool of persistent AudioSources, warmed up well before
        // first use (see WarmUp, called from GameBootstrap). A GameObject
        // spun up and Play()'d in the very same script call has, in this
        // project, already shown one "not actually registered yet" gotcha
        // (a freshly-added Collider invisible to Physics.OverlapSphere until
        // a frame passes) — reusing pre-existing sources sidesteps that
        // whole class of same-frame-creation issue for audio too.
        const int PoolSize = 12;
        static AudioSource[] _pool;
        static int _nextVoice;

        static void EnsurePool()
        {
            if (_pool != null) return;

            var host = new GameObject("SfxPool");
            Object.DontDestroyOnLoad(host);

            _pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var voice = new GameObject("SfxVoice" + i);
                voice.transform.SetParent(host.transform);
                var source = voice.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                _pool[i] = source;
            }
        }

        // Called once at game start so the pool exists (and is fully
        // initialized by Unity's audio backend) several frames before any
        // real gameplay sound is likely to fire.
        public static void WarmUp()
        {
            EnsureInit();
            EnsurePool();
        }

        // Static fields survive a Stop -> Play cycle (only a script
        // recompile resets them via domain reload) — but every scene object
        // from the previous session, including our pooled AudioSources and
        // the generated AudioClips, gets destroyed when Play mode stops.
        // Without this, EnsurePool()/EnsureInit()'s "already initialized"
        // guards would skip recreating them and silently hand out stale
        // references to destroyed objects on the next Play. Must be called
        // at the very start of every session (GameBootstrap.Setup does this).
        public static void ResetForNewSession()
        {
            _initialized = false;
            _pool = null;
            _nextVoice = 0;
        }

        static void EnsureInit()
        {
            if (_initialized) return;
            _initialized = true;

            _slam = ProceduralAudio.Impact(0.22f, 0.85f);
            _roar = ProceduralAudio.Impact(0.42f, 0.75f);
            _charge = ProceduralAudio.Impact(0.28f, 0.75f);
            _dash = ProceduralAudio.Noise(0.14f, 0.35f, 0.6f);

            _hurt = ProceduralAudio.Tone(220f, 0.14f, 0.5f, -100f);
            _gameOver = ProceduralAudio.Tone(200f, 0.9f, 0.55f, -120f);

            _enemyHit = ProceduralAudio.Noise(0.06f, 0.25f, 0.7f);
            _enemyDeath = ProceduralAudio.Noise(0.18f, 0.4f, 0.3f);
            _throw = ProceduralAudio.Noise(0.08f, 0.25f, 0.75f);

            _xpPickup = ProceduralAudio.Tone(880f, 0.09f, 0.3f, 500f);
            _powerupPickup = ProceduralAudio.Tone(660f, 0.24f, 0.45f, 350f);
            _levelUp = ProceduralAudio.Tone(523f, 0.4f, 0.5f, 350f);

            _roundClear = ProceduralAudio.Tone(440f, 0.55f, 0.5f, 220f);
            _roundStart = ProceduralAudio.Tone(300f, 0.3f, 0.5f, 160f);

            _rockExplosion = ProceduralAudio.Impact(0.5f, 0.9f);
        }

        // Non-spatial (spatialBlend = 0, no distance rolloff) — for a small
        // arena-sized game, 3D falloff risks making sounds too quiet to
        // notice rather than adding useful positioning. Uses a pre-warmed
        // pooled AudioSource rather than creating+playing one on the spot.
        static void Play(AudioClip clip, Vector3 position, float volume = 1f)
        {
            EnsureInit();
            EnsurePool();
            if (clip == null) return;

            var source = _pool[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _pool.Length;

            source.transform.position = position;
            source.clip = clip;
            source.volume = volume;
            source.Play();
        }

        static Vector3 ListenerPos()
        {
            var cam = Camera.main;
            return cam != null ? cam.transform.position : Vector3.zero;
        }

        public static void Slam(Vector3 pos) { EnsureInit(); Play(_slam, pos); }
        public static void Roar(Vector3 pos) { EnsureInit(); Play(_roar, pos); }
        public static void Charge(Vector3 pos) { EnsureInit(); Play(_charge, pos); }
        public static void Dash(Vector3 pos) { EnsureInit(); Play(_dash, pos, 0.7f); }

        public static void Hurt(Vector3 pos) { EnsureInit(); Play(_hurt, pos); }
        public static void GameOver() { EnsureInit(); Play(_gameOver, ListenerPos()); }

        public static void EnemyHit(Vector3 pos) { EnsureInit(); Play(_enemyHit, pos, 0.5f); }
        public static void EnemyDeath(Vector3 pos) { EnsureInit(); Play(_enemyDeath, pos, 0.6f); }
        public static void Throw(Vector3 pos) { EnsureInit(); Play(_throw, pos, 0.5f); }

        public static void XPPickup(Vector3 pos) { EnsureInit(); Play(_xpPickup, pos, 0.6f); }
        public static void PowerupPickup(Vector3 pos) { EnsureInit(); Play(_powerupPickup, pos); }
        public static void LevelUp() { EnsureInit(); Play(_levelUp, ListenerPos()); }

        public static void RoundClear() { EnsureInit(); Play(_roundClear, ListenerPos()); }
        public static void RoundStart() { EnsureInit(); Play(_roundStart, ListenerPos()); }

        public static void RockExplosion(Vector3 pos) { EnsureInit(); Play(_rockExplosion, pos); }
    }
}
