using UnityEngine;
using GorillaSurvivors.Core;

namespace GorillaSurvivors.Enemies
{
    // World-space HP bar floating above an enemy. Stays hidden until the
    // enemy has taken its first hit, so a full-health crowd doesn't clutter
    // the screen with bars nobody needs to read yet. Uses the camera's fixed
    // pitch (see CameraFollow.LabelRotation) instead of billboarding, same
    // trick as DamagePopup/pickup labels.
    public class EnemyHealthBar : MonoBehaviour
    {
        const float BarWidth = 0.85f;
        const float BarHeight = 0.14f;

        Transform _target;
        Transform _fill;
        float _heightAboveTarget;
        bool _shown;

        public static EnemyHealthBar Attach(Transform target, float heightAboveTarget)
        {
            var go = new GameObject("HealthBar");
            var bar = go.AddComponent<EnemyHealthBar>();
            bar._target = target;
            bar._heightAboveTarget = heightAboveTarget;
            bar.BuildVisual();
            go.SetActive(false);
            return bar;
        }

        void BuildVisual()
        {
            var bg = CreateQuad("Background", new Color(0.05f, 0.05f, 0.05f, 0.9f));
            bg.transform.SetParent(transform, false);
            bg.transform.localScale = new Vector3(BarWidth, BarHeight, 1f);

            var fillGO = CreateQuad("Fill", new Color(0.85f, 0.15f, 0.15f));
            fillGO.transform.SetParent(transform, false);
            fillGO.transform.localPosition = new Vector3(0f, 0f, -0.005f);
            fillGO.transform.localScale = new Vector3(BarWidth, BarHeight * 0.7f, 1f);
            _fill = fillGO.transform;
        }

        static GameObject CreateQuad(string name, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = MaterialCache.Get(color);
            return go;
        }

        public void SetFraction(float frac)
        {
            frac = Mathf.Clamp01(frac);

            if (!_shown)
            {
                _shown = true;
                gameObject.SetActive(true);
            }

            float halfWidth = BarWidth / 2f;
            _fill.localPosition = new Vector3(-halfWidth * (1f - frac), 0f, -0.005f);
            _fill.localScale = new Vector3(Mathf.Max(0.001f, BarWidth * frac), BarHeight * 0.7f, 1f);
        }

        void LateUpdate()
        {
            if (_target == null)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = _target.position + Vector3.up * _heightAboveTarget;
            transform.rotation = CameraFollow.LabelRotation;
        }
    }
}
