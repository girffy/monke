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
        // Two thirds of its original 13: three pulses of area damage that
        // also ignore shields was carrying fights on its own.
        public float BaseDamage = 8.7f;
        public float KnockbackForce = 10f;
        public float KnockbackDuration = 0.35f;

        // Tech tree.
        public int PulseCount = 3;              // "Drum Roll"
        public bool InvulnerableWhileBeating;   // "Unshakeable"
        public float MoveFraction;              // "Rolling Thunder"; 0 = rooted

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

        // Upper-arm directions for the RIGHT arm (the left mirrors on X),
        // paired with an elbow bend. The bend is the part that makes this
        // read as beating a chest rather than waving: the shoulder swings
        // the upper arm forward and in, and folding the forearm brings the
        // fist back against the chest. Rotating only the shoulder — as this
        // did before — can only ever point a straight arm outward, which is
        // why it looked like arms raised in the air.
        static readonly Vector3 RestDir = Vector3.down;
        static readonly Vector3 ArmsWideDir = new Vector3(0.80f, 0.16f, 0.30f).normalized;
        static readonly Vector3 ArmsChestDir = new Vector3(0.26f, -0.30f, 0.92f).normalized;
        const float RestElbow = 0f;
        const float ReadyElbow = 42f;
        const float StrikeElbow = 118f;

        public bool IsBeating => _isBeating;

        public float CooldownRemaining01()
        {
            float total = Cooldown * _stats.AbilityCooldownMultiplier;
            float remaining = Mathf.Max(0f, _nextReadyTime - Time.time);
            return total <= 0f ? 0f : Mathf.Clamp01(remaining / total);
        }

        // "Second Wind": clears the cooldown outright.
        public void ReadyNow() => _nextReadyTime = 0f;

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

            return UI.TouchControls.ConsumePress(UI.TouchButton.ChestBeat);
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

            // "Rolling Thunder" trades the root for a slow walk. The facing
            // is still locked either way so the animation doesn't spin.
            if (MoveFraction > 0f)
            {
                _controller.SpeedScale = MoveFraction;
                _controller.FacingLocked = true;
            }
            else
            {
                _controller.MovementLocked = true;
            }

            // "Unshakeable": covers the whole animation plus a moment after,
            // so the recovery frames aren't a free hit on a rooted player.
            if (InvulnerableWhileBeating)
            {
                _controller.GetComponent<PlayerHealth>()?.GrantInvulnerability(EstimatedDuration() + 0.15f);
            }

            if (_animator != null)
            {
                _animator.SuppressArms = true;
                _animator.StandUpright = true;
            }

            // Rear up onto the hind legs, both arms cocked out wide.
            yield return Pose(RestDir, ArmsWideDir, RestElbow, ReadyElbow, true, true, RiseTime, 0f, 0.20f, 0f, -12f);

            // Alternating single-arm strikes, right then left then right —
            // a gorilla drums its chest one fist at a time, and alternating
            // reads as drumming where both arms moving together reads as a
            // shrug. Each strike lands one of the three damage pulses.
            for (int i = 0; i < PulseCount; i++)
            {
                bool right = i % 2 == 0;

                // Fist in against the chest, body dipping into the blow.
                yield return Pose(ArmsWideDir, ArmsChestDir, ReadyElbow, StrikeElbow, right, !right, PoundTime, 0.20f, 0.16f, -12f, -6f);

                Pulse();
                StartCoroutine(HeadPulse());

                // ...and back out to the cocked position.
                yield return Pose(ArmsChestDir, ArmsWideDir, StrikeElbow, ReadyElbow, right, !right, ArmsOutTime, 0.16f, 0.20f, -6f, -12f);
                yield return new WaitForSeconds(HoldTime);
            }

            yield return Pose(ArmsWideDir, RestDir, ReadyElbow, RestElbow, true, true, SettleTime, 0.20f, 0f, -12f, 0f);
            EndBeat();
        }

        float EstimatedDuration()
        {
            return RiseTime + SettleTime + PulseCount * (PoundTime + ArmsOutTime + HoldTime);
        }

        void EndBeat()
        {
            SetArm(_armR, RestDir, RestElbow, false);
            SetArm(_armL, RestDir, RestElbow, true);
            _isBeating = false;
            if (_animator != null)
            {
                _animator.SuppressArms = false;
                _animator.StandUpright = false;
                _animator.BodyHeightOffset = 0f;
                _animator.BodyPitch = 0f;
            }
            _controller.MovementLocked = false;
            _controller.SpeedScale = 1f;
            _controller.FacingLocked = false;
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

        // Animates whichever arms are flagged; the others hold their pose,
        // which is what lets the strikes alternate.
        IEnumerator Pose(Vector3 fromDir, Vector3 toDir, float fromElbow, float toElbow,
            bool moveRight, bool moveLeft, float duration,
            float fromHeight, float toHeight, float fromPitch, float toPitch)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                var dir = Vector3.Slerp(fromDir, toDir, p);
                float elbow = Mathf.Lerp(fromElbow, toElbow, p);

                if (moveRight) SetArm(_armR, dir, elbow, false);
                if (moveLeft) SetArm(_armL, dir, elbow, true);

                if (_animator != null)
                {
                    _animator.BodyHeightOffset = Mathf.Lerp(fromHeight, toHeight, p);
                    _animator.BodyPitch = Mathf.Lerp(fromPitch, toPitch, p);
                }
                yield return null;
            }

            if (moveRight) SetArm(_armR, toDir, toElbow, false);
            if (moveLeft) SetArm(_armL, toDir, toElbow, true);
        }

        // Directions are authored for the right arm; the left mirrors on X
        // so both fists come in toward the chest rather than both pointing
        // the same way. The elbow is the limb's "Lower" joint (Blocky3DArt
        // builds every limb as shoulder -> Lower -> hand).
        void SetArm(Transform arm, Vector3 dir, float elbowDegrees, bool mirror)
        {
            if (arm == null) return;

            var d = mirror ? new Vector3(-dir.x, dir.y, dir.z) : dir;
            arm.localRotation = Quaternion.FromToRotation(Vector3.down, d);

            var lower = arm.Find("Lower");
            if (lower != null) lower.localRotation = Quaternion.Euler(-elbowDegrees, 0f, 0f);
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
