using UnityEngine;
using GorillaSurvivors.Core;

namespace GorillaSurvivors.Enemies
{
    // Stars spinning over a stunned man's head.
    //
    // Stun had no tell at all: a stunned enemy simply stopped, which from a
    // distance is indistinguishable from one that hasn't noticed you yet, or
    // one waiting out a knockback. Since the whole reason to pay for
    // Concussive is knowing you have a moment, the moment has to be visible
    // from across the arena without reading a health bar.
    public class StunMarker : MonoBehaviour
    {
        const int StarCount = 3;
        const float Radius = 0.34f;
        const float SpinSpeed = 220f;

        Transform _host;
        float _until;
        float _angle;
        float _baseHeight;

        public static void Show(Transform host, float until)
        {
            if (host == null) return;

            // One marker per enemy, extended rather than stacked — a second
            // stun landing on an already-stunned man should lengthen the
            // sign, not put a second set of stars inside the first.
            var existing = host.GetComponentInChildren<StunMarker>();
            if (existing != null)
            {
                existing._until = Mathf.Max(existing._until, until);
                return;
            }

            var go = new GameObject("StunMarker");
            go.transform.SetParent(host, false);

            var marker = go.AddComponent<StunMarker>();
            marker._host = host;
            marker._until = until;
            marker.Build();
        }

        void Build()
        {
            // Sits above whatever this enemy's head is. Every variant sizes
            // its own capsule, so that is the honest source for "how tall is
            // this one" rather than a constant that suits the Grunt.
            var collider = _host.GetComponent<CapsuleCollider>();
            _baseHeight = collider != null
                ? collider.center.y + collider.height * 0.5f + 0.42f
                : 2.1f;

            transform.localPosition = Vector3.up * _baseHeight;

            for (int i = 0; i < StarCount; i++)
            {
                var star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                star.name = "Star" + i;
                Object.Destroy(star.GetComponent<Collider>());
                star.transform.SetParent(transform, false);

                float a = i * 360f / StarCount;
                star.transform.localPosition = new Vector3(
                    Mathf.Cos(a * Mathf.Deg2Rad) * Radius, 0f, Mathf.Sin(a * Mathf.Deg2Rad) * Radius);
                star.transform.localScale = Vector3.one * 0.13f;

                var mr = star.GetComponent<MeshRenderer>();
                // Unlit, so they read the same from any angle and in the
                // shadow of the stands.
                mr.sharedMaterial = MaterialCache.GetUnlit(new Color(1f, 0.90f, 0.35f));
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
        }

        void Update()
        {
            if (Time.time >= _until)
            {
                Destroy(gameObject);
                return;
            }

            _angle += SpinSpeed * Time.deltaTime;
            transform.localRotation = Quaternion.Euler(0f, _angle, 0f);

            // A slight bob of the whole ring, set absolutely rather than
            // accumulated, so it doesn't drift off the top of the model.
            float bob = Mathf.Sin(Time.time * 5f) * 0.05f;
            transform.localPosition = new Vector3(0f, _baseHeight + bob, 0f);
        }
    }
}
