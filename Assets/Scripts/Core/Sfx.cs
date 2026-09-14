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
        static bool _initialized;

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
        }

        static void Play(AudioClip clip, Vector3 position, float volume = 1f)
        {
            EnsureInit();
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, volume);
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
    }
}
