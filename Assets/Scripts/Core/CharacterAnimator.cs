using UnityEngine;

namespace GorillaSurvivors.Core
{
    // Procedural walk/idle animation for the primitive-built characters.
    // Drives the "ArmL"/"ArmR"/"LegL"/"LegR" pivots that Blocky3DArt creates,
    // plus a body bob and lean on the model root, from the owner's actual
    // movement speed — no clips or rigs involved.
    //
    // Attacks that pose the arms themselves (the slam, the swipe) set
    // SuppressArms while they run so the two aren't fighting over the same
    // transforms.
    public class CharacterAnimator : MonoBehaviour
    {
        [Header("Tuning")]
        public float StrideFrequency = 2.6f;   // cycles per unit travelled
        public float LegSwing = 38f;           // degrees
        public float ArmSwing = 30f;
        public float BobHeight = 0.06f;
        public float LeanDegrees = 12f;
        public float IdleBreathAmplitude = 0.02f;
        public float ReferenceSpeed = 4.5f;    // speed treated as "full stride"

        // Set by whatever is posing the arms this frame (PlayerAttack,
        // QuickSwipeAttack). While true the animator leaves arms alone and
        // eases them back in once released.
        public bool SuppressArms { get; set; }

        // Abilities express body motion (a slam crouch, a charge lean) by
        // setting these rather than writing to the model transform directly —
        // the animator stays the single owner of the model's local pose, so
        // nothing gets stomped depending on update order.
        public float BodyHeightOffset { get; set; }
        public float BodyPitch { get; set; }
        public float BodyYaw { get; set; }

        Transform _model;
        Transform _armL, _armR, _legL, _legR;
        Rigidbody _rb;

        Vector3 _modelBasePos;
        float _phase;
        float _armBlend = 1f;
        float _leanBlend;
        Quaternion _appliedLean = Quaternion.identity;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _model = transform.childCount > 0 ? transform.GetChild(0) : null;
            if (_model == null) return;

            _modelBasePos = _model.localPosition;
            _armL = _model.Find("ArmL");
            _armR = _model.Find("ArmR");
            _legL = _model.Find("LegL");
            _legR = _model.Find("LegR");
        }

        void LateUpdate()
        {
            if (_model == null) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            float speed = 0f;
            if (_rb != null)
            {
                var v = _rb.linearVelocity;
                v.y = 0f;
                speed = v.magnitude;
            }

            float gait = Mathf.Clamp01(speed / Mathf.Max(0.01f, ReferenceSpeed));

            // Advance the stride by distance travelled, not by time, so the
            // feet don't skate when the character speeds up or slows down.
            _phase += speed * StrideFrequency * Time.deltaTime;

            float swing = Mathf.Sin(_phase);
            float bob = Mathf.Abs(Mathf.Cos(_phase));

            if (_legL != null) _legL.localRotation = Quaternion.Euler(swing * LegSwing * gait, 0f, 0f);
            if (_legR != null) _legR.localRotation = Quaternion.Euler(-swing * LegSwing * gait, 0f, 0f);

            // Ease the arms back to the walk cycle after an attack releases
            // them, instead of snapping mid-swing.
            _armBlend = Mathf.MoveTowards(_armBlend, SuppressArms ? 0f : 1f, Time.deltaTime * 6f);
            if (_armBlend > 0.001f)
            {
                float armAmount = ArmSwing * gait * _armBlend;
                if (_armL != null) _armL.localRotation = Quaternion.Euler(-swing * armAmount, 0f, 0f);
                if (_armR != null) _armR.localRotation = Quaternion.Euler(swing * armAmount, 0f, 0f);
            }

            // Idle breathing keeps a standing character from looking frozen.
            float idle = Mathf.Sin(Time.time * 2.2f) * IdleBreathAmplitude * (1f - gait);
            _model.localPosition = _modelBasePos + new Vector3(0f, bob * BobHeight * gait + idle + BodyHeightOffset, 0f);

            // Lean into the direction of travel. PlayerController (aim
            // facing) and EnemyAI (chase facing) both own the model's
            // rotation, so the lean is composed on top of whatever they set
            // — and last frame's lean is undone first, so it can never
            // accumulate if nobody reasserts the facing on some frame.
            _model.localRotation = _model.localRotation * Quaternion.Inverse(_appliedLean);

            _leanBlend = Mathf.MoveTowards(_leanBlend, gait, Time.deltaTime * 4f);
            var lean = Quaternion.identity;
            if (_leanBlend > 0.001f && _rb != null)
            {
                Vector3 dir = _rb.linearVelocity;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                {
                    // Velocity expressed in the model's own (unleaned) space:
                    // +Z is "forward as drawn", so moving that way pitches
                    // nose-down and strafing rolls into the turn.
                    Vector3 local = _model.InverseTransformDirection(dir.normalized);
                    lean = Quaternion.Euler(local.z * LeanDegrees * _leanBlend, 0f, -local.x * LeanDegrees * _leanBlend);
                }
            }

            var pose = lean * Quaternion.Euler(BodyPitch, BodyYaw, 0f);
            _model.localRotation = _model.localRotation * pose;
            _appliedLean = pose;
        }
    }
}
