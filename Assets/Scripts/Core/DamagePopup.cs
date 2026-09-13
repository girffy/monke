using UnityEngine;

namespace GorillaSurvivors.Core
{
    // Floating "-N" text that pops up and fades whenever the player takes damage.
    public class DamagePopup : MonoBehaviour
    {
        const float Life = 0.7f;
        static readonly Vector3 Velocity = new Vector3(0f, 1.2f, 0f);

        float _t;
        TextMesh _mesh;

        public static void Spawn(Vector3 worldPos, float amount)
        {
            var go = new GameObject("DamagePopup");
            go.transform.position = worldPos + new Vector3(0f, 0.6f, 0f);

            var mesh = go.AddComponent<TextMesh>();
            mesh.text = $"-{Mathf.CeilToInt(amount)}";
            mesh.fontSize = 48;
            mesh.characterSize = 0.11f;
            mesh.anchor = TextAnchor.LowerCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = new Color(1f, 0.25f, 0.2f);
            mesh.fontStyle = FontStyle.Bold;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sortingOrder = 20;

            var popup = go.AddComponent<DamagePopup>();
            popup._mesh = mesh;
        }

        void Update()
        {
            _t += Time.deltaTime;
            transform.position += Velocity * Time.deltaTime;

            var c = _mesh.color;
            c.a = Mathf.Lerp(1f, 0f, _t / Life);
            _mesh.color = c;

            if (_t >= Life) Destroy(gameObject);
        }
    }
}
