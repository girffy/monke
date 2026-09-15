using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using GorillaSurvivors.Core;
using GorillaSurvivors.Enemies;

namespace GorillaSurvivors.Player.Abilities
{
    // Unlockable crowd-control ability: press Q to rear up and beat the
    // chest. Three separate shockwave pulses go off over roughly two
    // seconds, each damaging and shoving back everything around the player.
    //
    // Movement rules match the LMB slam deliberately: it roots the player
    // for the duration, and a dash cancels out of it early — the difference
    // is that committing to the full animation is what pays out all three
    // pulses, so it's a real decision rather than a free panic button.
    public class ChestBeatAbility : MonoBehaviour
    {
        public bool Unlocked;
        public float Cooldown = 7f;
        public float Radius = 4.5f;
        public float BaseDamage = 13f;
        public float KnockbackForce = 10f;
        public float KnockbackDuration = 0.35f;

        public const int PulseCount = 3;
        const float RiseTime = 0.18f;
        const float ArmsOutTime = 0.16f;
        const float PoundTime = 0.10f;
        const float HoldTime = 0.29f;
        const float SettleTime = 0.20f;

        PlayerStats _stats;
        PlayerController _controller;
        CharacterAnimator _animator;
        Transform _head;
        Transform _armL, _armR;

        float _nextReadyTime;
        bool _isBeating;
        int _pulsesFired;
        Coroutine _routine;

        static readonly Collider[] HitBuffer = new Collider[64];

        // Arms swing wide, then slam in against the chest.
        static readonly Vector3 ArmsWideDir = new Vector3(0.78f, 0.42f, 0.18f).normalized;
        static readonly Vector3 ArmsInDir = new Vector3(-0.22f, 0.50f, 0.62f).normalized;

        public bool IsBeating => _isBeating;

        public float CooldownRemaining01()
        {
            float total = Cooldown * _stats.AbilityCooldownMultiplier;
            float remaining = Mathf.Max(0f, _nextReadyTime - Time.time);
            return total <= 0f ? 0f : Mathf.Clamp01(remaining / total);
        }

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _controller = GetComponent<PlayerController>();
            _animator = GetComponent<CharacterAnimator>();
            var model = transform.Find("GorillaModel");
            _head = model != null ? model.Find("Head") : null;
            _armL = model != null ? model.Find("ArmL") : null;
            _armR = model != null ? model.Find("ArmR") : null;
        }

        void Update()
        {
            if (!Unlocked || _isBeating) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
            if (Time.time < _nextReadyTime) return;
            if (!WasPressed()) return;

            _nextReadyTime = Time.time + Cooldown * _stats.AbilityCooldownMultiplier;
            _routine = StartCoroutine(BeatSequence());
        }

        bool WasPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.qKey.wasPressedThisFrame) return true;

            var gp = Gamepad.current;
            if (gp != null && gp.buttonNorth.wasPressedThisFrame) return true;

            return false;
        }

        // Called by PlayerController when dash is pressed mid-beat. Mirrors
        // PlayerAttack.TryCancelWithDash: the pulse that was already winding
        // up still lands, the rest of the animation is abandoned.
        public bool TryCancelWithDash()
        {
            if (!_isBeating) return false;

            if (_routine != null) StopCoroutine(_routine);
            if (_pulsesFired < PulseCount) Pulse();

            EndBeat();
            return true;
        }

        IEnumerator BeatSequence()
        {
            _isBeating = true;
            _pulsesFired = 0;
            _controller.MovementLocked = true;
            if (_animator != null) _animator.SuppressArms = true;

            // Rear up onto the hind legs.
            yield return Pose(Vector3.down, ArmsWideDir, RiseTime, 0f, 0.20f, 0f, -12f);

            for (int i = 0; i < PulseCount; i++)
            {
                yield return Pose(ArmsWideDir, ArmsInDir, PoundTime, 0.20f, 0.14f, -12f, -4f);

                Pulse();
                StartCoroutine(HeadPulse());

                yield return Pose(ArmsInDir, ArmsWideDir, ArmsOutTime, 0.14f, 0.20f, -4f, -12f);
                yield return new WaitForSeconds(HoldTime);
            }

            yield return Pose(ArmsWideDir, Vector3.down, SettleTime, 0.20f, 0f, -12f, 0f);
            EndBeat();
        }

        void EndBeat()
        {
            SetArms(Vector3.down);
            _isBeating = false;
            if (_animator != null)
            {
                _animator.SuppressArms = false;
                _animator.BodyHeightOffset = 0f;
                _animator.BodyPitch = 0f;
            }
            _controller.MovementLocked = false;
        }

        void Pulse()
        {
            _pulsesFired++;

            float damage = BaseDamage * _stats.LevelDamageBonus * _stats.DamageMultiplier;
            float radius = Radius * _stats.AreaMultiplier;

            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, HitBuffer);
            for (int i = 0; i < count; i++)
            {
                var enemyHealth = HitBuffer[i].GetComponentInParent<EnemyHealth>();
                if (enemyHealth == null) continue;

                // No direction passed: a shockwave from all sides ignores the
                // Shieldman's frontal block, which is the point of having it.
                enemyHealth.TakeDamage(damage);

                var enemyAI = HitBuffer[i].GetComponentInParent<EnemyAI>();
                if (enemyAI != null)
                {
                    Vector3 away = enemyAI.transform.position - transform.position;
                    away.y = 0f;
                    if (away.sqrMagnitude < 0.0001f) away = Random.insideUnitSphere;
                    enemyAI.ApplyKnockback(away.normalized * KnockbackForce, KnockbackDuration);
                }
            }

            Sfx.Roar(transform.position);
            CameraShake.Shake(0.26f, 0.24f);
            SpawnRing(radius, new Color(1f, 0.95f, 0.6f), 0.28f);
            SpawnRing(radius * 1.05f, new Color(1f, 1f, 0.88f), 0.15f);
        }

        void SpawnRing(float radius, Color color, float duration)
        {
            var go = Blocky3DArt.SwipeDisc(color);
            go.transform.position = transform.position + Vector3.up * 0.05f;
            go.transform.localScale = new Vector3(0.1f, 0.02f, 0.1f);
            go.AddComponent<GorillaSurvivors.Environment.ExpandingDisc>().Play(radius * 2f, duration);
        }

        IEnumerator Pose(Vector3 fromDir, Vector3 toDir, float duration,
            float fromHeight, float toHeight, float fromPitch, float toPitch)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                SetArms(Vector3.Slerp(fromDir, toDir, p));
                if (_animator != null)
                {
                    _animator.BodyHeightOffset = Mathf.Lerp(fromHeight, toHeight, p);
                    _animator.BodyPitch = Mathf.Lerp(fromPitch, toPitch, p);
                }
                yield return null;
            }
            SetArms(toDir);
        }

        // Mirrored on X so the arms pound in toward the chest from both
        // sides rather than both pointing the same way.
        void SetArms(Vector3 dir)
        {
            if (_armL != null) _armL.localRotation = Quaternion.FromToRotation(Vector3.down, new Vector3(-dir.x, dir.y, dir.z));
            if (_armR != null) _armR.localRotation = Quaternion.FromToRotation(Vector3.down, dir);
        }

        IEnumerator HeadPulse()
        {
            if (_head == null) yield break;

            var baseScale = _head.localScale;
            float duration = 0.18f;
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float pulse = 1f + 0.22f * Mathf.Sin(t / duration * Mathf.PI);
                _head.localScale = baseScale * pulse;
                yield return null;
            }

            _head.localScale = baseScale;
        }
    }
}
