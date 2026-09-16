using UnityEngine;
using GorillaSurvivors.Core;

namespace GorillaSurvivors.Environment
{
    // A walled colosseum the fight is confined to. The map used to be
    // unbounded, which meant the optimal play was to pick a direction and
    // run forever, kiting the whole horde behind you. Walls turn that into
    // circling and positioning inside a fixed space.
    //
    // Four gates at the cardinal points are where the horde comes in when
    // you can see them (see EnemySpawner) — the player is never let out
    // through them, since an exit would restore the run-away strategy.
    public class Arena : MonoBehaviour
    {
        public static Arena Instance { get; private set; }

        // Roughly three screen widths across at the fixed camera height.
        public float Radius = 30f;
        // Deliberately low. The camera sits behind and above the player, so
        // at the near edge of the arena it looks in from OUTSIDE the ring —
        // a tall wall puts itself between the camera and the gorilla.
        public float WallHeight = 1.7f;

        public Vector3 Center => transform.position;

        public static readonly Vector3[] GateDirections =
        {
            Vector3.forward, Vector3.right, Vector3.back, Vector3.left,
        };

        // Half-width of each gate opening, in degrees around the ring.
        const float GateHalfAngle = 11f;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public Vector3 GatePosition(int index)
        {
            return Center + GateDirections[index] * (Radius - 0.8f);
        }

        // Keeps a point inside the ring, leaving room for the body radius.
        public Vector3 ClampInside(Vector3 position, float margin)
        {
            Vector3 flat = position - Center;
            flat.y = 0f;

            float limit = Mathf.Max(0.5f, Radius - margin);
            if (flat.sqrMagnitude <= limit * limit) return position;

            Vector3 clamped = Center + flat.normalized * limit;
            clamped.y = position.y;
            return clamped;
        }

        public bool IsInside(Vector3 position, float margin)
        {
            Vector3 flat = position - Center;
            flat.y = 0f;
            float limit = Mathf.Max(0.5f, Radius - margin);
            return flat.sqrMagnitude <= limit * limit;
        }

        public bool IsGateOnScreen(int index, Camera cam)
        {
            if (cam == null) return false;

            Vector3 viewport = cam.WorldToViewportPoint(GatePosition(index));
            return viewport.z > 0f
                && viewport.x > -0.05f && viewport.x < 1.05f
                && viewport.y > -0.05f && viewport.y < 1.05f;
        }

        public void Build()
        {
            var stone = new Color(0.56f, 0.53f, 0.47f);
            var stoneDark = new Color(0.44f, 0.42f, 0.38f);
            var stoneLight = new Color(0.66f, 0.63f, 0.56f);
            var sand = new Color(0.55f, 0.48f, 0.36f);

            // Blocks around the ring, skipping the gate arcs. Each block is
            // turned to face the centre so the wall reads as a curve rather
            // than a polygon.
            const int segments = 96;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * 360f / segments;
                if (IsGateAngle(angle)) continue;

                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Vector3 basePos = Center + dir * Radius;

                var block = CreateBlock("Wall" + i, basePos + Vector3.up * (WallHeight * 0.5f),
                    new Vector3(2.4f, WallHeight, 1.4f), i % 2 == 0 ? stone : stoneDark);
                block.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

                // Capped rail along the top, slightly proud of the wall.
                var rail = CreateBlock("Rail" + i, basePos + Vector3.up * (WallHeight + 0.22f),
                    new Vector3(2.5f, 0.44f, 1.8f), stoneLight);
                rail.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

                // A second tier stepped outward reads as stands around the
                // arena instead of a bare fence.
                if (i % 2 == 0)
                {
                    var tier = CreateBlock("Tier" + i, basePos + dir * 1.7f + Vector3.up * (WallHeight * 0.5f + 0.35f),
                        new Vector3(2.4f, WallHeight + 0.7f, 1.7f), i % 4 == 0 ? stoneDark : stone);
                    tier.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                }
            }

            for (int g = 0; g < GateDirections.Length; g++)
            {
                BuildGate(GateDirections[g], stone, stoneLight, sand);
            }
        }

        static bool IsGateAngle(float angle)
        {
            for (int g = 0; g < GateDirections.Length; g++)
            {
                float gateAngle = Mathf.Atan2(GateDirections[g].x, GateDirections[g].z) * Mathf.Rad2Deg;
                if (Mathf.Abs(Mathf.DeltaAngle(angle, gateAngle)) < GateHalfAngle) return true;
            }
            return false;
        }

        void BuildGate(Vector3 dir, Color stone, Color stoneLight, Color sand)
        {
            Vector3 center = Center + dir * Radius;
            Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
            var rotation = Quaternion.LookRotation(dir, Vector3.up);

            for (int s = -1; s <= 1; s += 2)
            {
                var pillar = CreateBlock("GatePillar", center + side * (s * 3.1f) + Vector3.up * (WallHeight * 0.7f),
                    new Vector3(1.5f, WallHeight * 1.4f, 2.0f), stone);
                pillar.transform.rotation = rotation;

                var cap = CreateBlock("GateCap", center + side * (s * 3.1f) + Vector3.up * (WallHeight * 1.4f + 0.18f),
                    new Vector3(1.9f, 0.36f, 2.4f), stoneLight);
                cap.transform.rotation = rotation;
            }

            // No lintel across the opening: a beam there sits at exactly
            // camera height whenever the player stands in a gate.

            // Sand threshold so the opening reads as a way in, not a hole.
            var threshold = CreateBlock("GateFloor", center - dir * 0.6f + Vector3.up * 0.03f,
                new Vector3(5.6f, 0.06f, 2.6f), sand);
            threshold.transform.rotation = rotation;
        }

        GameObject CreateBlock(string name, Vector3 position, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            // The player is kept in by a position clamp rather than by
            // colliders: 200-odd wall colliders would be a lot of physics
            // for a boundary a clamp handles exactly.
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = MaterialCache.Get(color);
            return go;
        }
    }
}
