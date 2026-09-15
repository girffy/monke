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

        [Header("Knuckle walk")]
        // Quadruped stance: a gorilla stands and moves on its knuckles, and
        // only rears up for moves that need it (see StandUpright).
        public bool KnuckleWalk;
        public float KnuckleWalkPitch = 30f;    // degrees nose-down on all fours
        public float KnuckleWalkCrouch = -0.16f;// body drops as it goes down
        public float KnuckleArmForward = 20f;   // arms reach ahead to plant

        // Set by moves performed standing (the overhead slam, the chest
        // beat). The stance blends rather than snaps, so rearing up and
        // dropping back onto the knuckles both read as motion.
        public bool StandUpright { get; set; }

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
        float _uprightBlend;
        Quaternion _baseRotation = Quaternion.identity;
        Quaternion _lastWritten = Quaternion.identity;
        bool _hasWritten;

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

            _uprightBlend = Mathf.MoveTowards(_uprightBlend, StandUpright ? 1f : 0f, Time.deltaTime * 7f);
            float stance = KnuckleWalk ? 1f - _uprightBlend : 0f;

            if (_legL != null) _legL.localRotation = Quaternion.Euler(swing * LegSwing * gait, 0f, 0f);
            if (_legR != null) _legR.localRotation = Quaternion.Euler(-swing * LegSwing * gait, 0f, 0f);

            // Suppression is immediate but the return eases. Blending OUT
            // would mean the animator kept partially overwriting an attack's
            // arm pose for the first few frames of every swing — the
            // animator runs in LateUpdate, so it gets the last word.
            _armBlend = SuppressArms ? 0f : Mathf.MoveTowards(_armBlend, 1f, Time.deltaTime * 6f);
            if (_armBlend > 0.001f)
            {
                float armAmount = ArmSwing * gait * _armBlend;
                // Negative pitch swings a downward-hanging limb forwards, so
                // the knuckle reach is subtracted to plant the hands ahead.
                float armBase = -KnuckleArmForward * stance * _armBlend;
                if (_armL != null) _armL.localRotation = Quaternion.Euler(armBase - swing * armAmount, 0f, 0f);
                if (_armR != null) _armR.localRotation = Quaternion.Euler(armBase + swing * armAmount, 0f, 0f);
            }

            // Idle breathing keeps a standing character from looking frozen.
            float idle = Mathf.Sin(Time.time * 2.2f) * IdleBreathAmplitude * (1f - gait);
            float knuckleCrouch = KnuckleWalkCrouch * stance;
            _model.localPosition = _modelBasePos + new Vector3(0f, bob * BobHeight * gait + idle + BodyHeightOffset + knuckleCrouch, 0f);

            // Lean into the direction of travel, composed on top of the
            // facing that PlayerController (aim) / EnemyAI (chase) set.
            //
            // Those writers assign a WORLD rotation every frame, which wipes
            // whatever pose was applied last frame. So rather than trying to
            // undo the previous pose off the transform (which silently
            // cancelled the pose out entirely when a writer had already
            // replaced it), the clean facing is tracked here: if the
            // rotation still matches what this component wrote last frame,
            // nobody has touched it and the remembered facing is reused;
            // otherwise the current value IS a fresh facing.
            if (!_hasWritten || Quaternion.Angle(_model.localRotation, _lastWritten) > 0.01f)
            {
                _baseRotation = _model.localRotation;
            }

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
                    Vector3 local = Quaternion.Inverse(_baseRotation) * _model.parent.InverseTransformDirection(dir.normalized);
                    lean = Quaternion.Euler(local.z * LeanDegrees * _leanBlend, 0f, -local.x * LeanDegrees * _leanBlend);
                }
            }

            float knucklePitch = KnuckleWalkPitch * stance;
            var pose = lean * Quaternion.Euler(BodyPitch + knucklePitch, BodyYaw, 0f);
            _model.localRotation = _baseRotation * pose;
            _lastWritten = _model.localRotation;
            _hasWritten = true;
        }
    }
}
