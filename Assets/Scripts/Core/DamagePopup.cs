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
            Spawn(worldPos, amount, new Color(1f, 0.25f, 0.2f), 0.11f);
        }

        public static void Spawn(Vector3 worldPos, float amount, Color color, float size)
        {
            var go = new GameObject("DamagePopup");
            // Scatter sideways a little so several numbers in the same moment
            // don't stack into one unreadable smear.
            go.transform.position = worldPos + new Vector3(Random.Range(-0.25f, 0.25f), 1.6f, 0f);
            go.transform.rotation = CameraFollow.LabelRotation;

            var mesh = go.AddComponent<TextMesh>();
            mesh.text = $"-{Mathf.CeilToInt(amount)}";
            mesh.fontSize = 48;
            mesh.characterSize = size;
            mesh.anchor = TextAnchor.LowerCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
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
