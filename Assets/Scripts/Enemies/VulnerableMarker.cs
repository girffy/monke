using UnityEngine;
using GorillaSurvivors.Core;

namespace GorillaSurvivors.Enemies
{
    // A red ring on the ground under anything the gorilla has raked.
    //
    // Vulnerable is worth taking only if you can act on it — line the slam
    // up on the marked ones, drop the dung where they are standing — and
    // that requires seeing at a glance who is marked in a crowd of a hundred.
    // On the GROUND rather than over the head, because it has to be readable
    // in a pile of overlapping bodies where heads are hidden behind each
    // other.
    public class VulnerableMarker : MonoBehaviour
    {
        float _until;
        float _duration;
        Transform _disc;

        public static void Show(Transform host, float until)
        {
            if (host == null) return;

            var existing = host.GetComponentInChildren<VulnerableMarker>();
            if (existing != null)
            {
                existing._duration = Mathf.Max(existing._duration, until - Time.time);
                existing._until = Mathf.Max(existing._until, until);
                return;
            }

            var go = new GameObject("VulnerableMarker");
            go.transform.SetParent(host, false);
            go.transform.localPosition = Vector3.up * 0.04f;

            var marker = go.AddComponent<VulnerableMarker>();
            marker._until = until;
            marker._duration = Mathf.Max(0.01f, until - Time.time);
            marker._disc = Blocky3DArt.SwipeDisc(new Color(0.95f, 0.25f, 0.22f)).transform;
            marker._disc.SetParent(go.transform, false);
            marker._disc.localPosition = Vector3.zero;
            marker._disc.localScale = new Vector3(1.05f, 0.02f, 1.05f);
            return;
        }

        void Update()
        {
            float remaining = _until - Time.time;
            if (remaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            // Shrinks as it runs out, so the ring doubles as the timer — how
            // long is left is the same read as who is marked.
            if (_disc != null)
            {
                float size = Mathf.Lerp(0.55f, 1.05f, remaining / _duration);
                _disc.localScale = new Vector3(size, 0.02f, size);
            }
        }
    }
}
