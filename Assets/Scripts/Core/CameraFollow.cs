using UnityEngine;

namespace GorillaSurvivors.Core
{
    public class CameraFollow : MonoBehaviour
    {
        public Transform Target;
        public float SmoothTime = 0.15f;

        Vector3 _velocity;

        void LateUpdate()
        {
            if (Target == null) return;

            var targetPos = new Vector3(Target.position.x, Target.position.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, SmoothTime);
        }
    }
}
