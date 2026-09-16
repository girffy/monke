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

        // Roughly a screen and a half across at the fixed camera height. The
        // first pass at twice this left too much empty floor to retreat into,
        // which is the kiting problem the walls were meant to remove.
        public float Radius = 15f;
        // Deliberately low. The camera sits behind and above the player, so
        // at the near edge of the arena it looks in from OUTSIDE the ring —
        // a tall wall puts itself between the camera and the gorilla.
        public float WallHeight = 1.7f;

        public Vector3 Center => transform.position;

        public static readonly Vector3[] GateDirections =
        {
            Vector3.forward, Vector3.right, Vector3.back, Vector3.left,
        };

        // Half-width of each gate opening, in degrees around the ring. Wider
        // than it looks: the ring got half as big, so the same opening in
        // metres is twice the angle.
        const float GateHalfAngle = 13f;

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

        static readonly Color Stone = new Color(0.60f, 0.57f, 0.50f);
        static readonly Color StoneDark = new Color(0.47f, 0.44f, 0.40f);
        static readonly Color StoneLight = new Color(0.72f, 0.69f, 0.61f);
        static readonly Color Sandstone = new Color(0.69f, 0.60f, 0.44f);

        HideWhenBlockingCamera _occluders;

        public void Build()
        {
            _occluders = gameObject.AddComponent<HideWhenBlockingCamera>();

            BuildFloor();
            BuildRing();

            for (int g = 0; g < GateDirections.Length; g++)
            {
                BuildGate(GateDirections[g]);
            }
        }

        // A raked-sand floor laid over the grass. Rendered as a disc rather
        // than as part of the ground plane so the grass still shows outside
        // the walls, which is what sells the ring as a built structure
        // dropped into the landscape.
        void BuildFloor()
        {
            // The apron first and lowest, so it shows only as a dark stone
            // border around the sand.
            AddFloorDisc("FloorRim", Radius + 1.1f, 0.22f, -0.015f,
                MaterialCache.Get(new Color(0.38f, 0.35f, 0.31f)));

            // Deliberately a SOLID slab rather than a paper-thin disc. A
            // near-zero-thickness cylinder sitting a hair above the ground
            // plane z-fights with it and self-shadows along its facets,
            // which showed up as bright radial wedges fanning across the
            // floor — the arena looked like a broken texture.
            //
            // The top sits at EXACTLY y=0, which is where the fighters' feet
            // are and below every ground effect in the game (swipe and slam
            // discs at 0.05, dung patches at 0.04, the bomb warning ring at
            // 0.05). Raising it clear of the grass instead buried all of
            // them and left everyone standing shin-deep in sand; the grass
            // plane is dropped slightly to make room (Blocky3DArt.GroundY).
            AddFloorDisc("ArenaFloor", Radius, 0.3f, 0f,
                MaterialCache.GetTextured(ProceduralTextures.Sand(), Color.white, new Vector2(Radius / 3f, Radius / 3f)));
        }

        void AddFloorDisc(string name, float radius, float thickness, float topY, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.position = Center + Vector3.up * (topY - thickness * 0.5f);
            // Unity's cylinder mesh is TWO units tall, so the vertical scale
            // is half the thickness we want. Passing the thickness straight
            // through put the slab's surface 15cm above where the maths said
            // it was, which buried every ground effect (swipe discs, dung
            // patches, bomb warning rings all live between 0.04 and 0.07)
            // and left the fighters standing shin-deep in sand.
            go.transform.localScale = new Vector3(radius * 2f, thickness * 0.5f, radius * 2f);
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            // Receives shadows (props and fighters need to sit on it) but
            // casts none — a disc lying on the ground has nothing to cast on.
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // Blocks around the ring, skipping the gate arcs. Each block is
        // turned to face the centre so the wall reads as a curve rather than
        // a polygon.
        void BuildRing()
        {
            const int segments = 64;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * 360f / segments;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Vector3 basePos = Center + dir * Radius;
                var facing = Quaternion.LookRotation(dir, Vector3.up);

                // Over a gate, the seating carries straight across rather
                // than stopping. Without this the archways sat in a gap in
                // the ring like porches stuck on the outside of it; the
                // passage should read as bored THROUGH the stands.
                if (IsGateAngle(angle))
                {
                    BuildStands(i, dir, basePos, facing, GateStructureTop);
                    continue;
                }

                var block = CreateBlock("Wall" + i, basePos + Vector3.up * (WallHeight * 0.5f),
                    new Vector3(1.7f, WallHeight, 1.4f), i % 2 == 0 ? Stone : StoneDark);
                block.transform.rotation = facing;

                // Capped rail along the top, slightly proud of the wall.
                var rail = CreateBlock("Rail" + i, basePos + Vector3.up * (WallHeight + 0.20f),
                    new Vector3(1.8f, 0.40f, 1.8f), StoneLight);
                rail.transform.rotation = facing;

                BuildStands(i, dir, basePos, facing);
            }
        }

        // Tiered stands stepping up and outward behind the wall.
        //
        // Their height is driven by how far around the ring the segment is
        // from the camera. The camera is fixed: it sits south of the player
        // looking north, so the southern arc is always the near edge and
        // always between the camera and the fight. Stands there would block
        // the view, so they taper to nothing across the south and rise to
        // full height across the north — you look over a low rail in the
        // foreground at a full amphitheatre on the far side.
        void BuildStands(int index, Vector3 dir, Vector3 basePos, Quaternion facing, float baseY = 0f)
        {
            float northness = Vector3.Dot(dir, Vector3.forward);
            float tall = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.15f, 0.55f, northness));
            if (tall <= 0.02f) return;

            basePos += Vector3.up * baseY;

            // Five tiers rather than three: the ring read as a low fence with
            // a step behind it. A colosseum's whole character is that it
            // keeps going up — and going higher costs nothing, because the
            // tapering above already keeps the near side out of the shot.
            const int tiers = 5;
            for (int t = 0; t < tiers; t++)
            {
                float outward = 1.5f + t * 1.5f;
                float height = (WallHeight + 0.8f + t * 1.5f) * tall;
                if (height < 0.3f) continue;

                var tier = CreateBlock("Tier" + index + "_" + t,
                    basePos + dir * outward + Vector3.up * (height * 0.5f),
                    new Vector3(1.8f, height, 1.6f),
                    (index + t) % 2 == 0 ? Stone : StoneDark);
                tier.transform.rotation = facing;

                // A crowd watching the fight. Small unlit cubes in a row are
                // enough at this distance, and they do more for "colosseum"
                // than any amount of extra stonework.
                if (tall > 0.35f && index % 2 == 0) BuildCrowdRow(basePos + dir * outward, dir, height, facing);
            }
        }

        static readonly Color[] CrowdTones =
        {
            new Color(0.78f, 0.30f, 0.26f), new Color(0.30f, 0.38f, 0.66f),
            new Color(0.82f, 0.72f, 0.40f), new Color(0.34f, 0.56f, 0.36f),
            new Color(0.60f, 0.40f, 0.62f), new Color(0.85f, 0.82f, 0.76f),
        };

        void BuildCrowdRow(Vector3 tierPos, Vector3 dir, float tierHeight, Quaternion facing)
        {
            Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
            for (int s = -1; s <= 1; s++)
            {
                var person = CreateBlock("Crowd", tierPos + side * (s * 0.55f) + Vector3.up * (tierHeight + 0.22f),
                    new Vector3(0.28f, 0.44f, 0.28f), CrowdTones[Random.Range(0, CrowdTones.Length)]);
                person.transform.rotation = facing;
                var head = CreateBlock("CrowdHead", tierPos + side * (s * 0.55f) + Vector3.up * (tierHeight + 0.54f),
                    Vector3.one * 0.2f, new Color(0.72f, 0.56f, 0.42f));
                head.transform.rotation = facing;
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

        // Half-width of the gate opening, and the height its arch springs
        // from — the arch is a semicircle of that radius on top.
        const float GateHalfWidth = 2.1f;
        const float GateSpring = 1.9f;
        // How far the passage runs outward before it is stopped by darkness.
        // Kept inside the stands' own footprint (they step out to 7.5) so
        // the gate is a hole in the structure, not a lump on its side.
        const float TunnelDepth = 4.5f;

        // Height of the tunnel bore, and the top of the whole gate structure
        // — where the seating above it starts.
        const float TunnelHeight = GateSpring + GateHalfWidth + 0.5f;
        const float GateStructureTop = TunnelHeight + 0.7f;

        void BuildGate(Vector3 dir)
        {
            Vector3 center = Center + dir * Radius;
            Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
            var rotation = Quaternion.LookRotation(dir, Vector3.up);

            for (int s = -1; s <= 1; s += 2)
            {
                // Directly under the arch's springing (see BuildArch), so the
                // arch lands on the pillars instead of floating past them.
                Vector3 jamb = center + side * (s * (GateHalfWidth + 0.28f));

                var pillar = CreateBlock("GatePillar", jamb + Vector3.up * (GateSpring * 0.5f),
                    new Vector3(1.1f, GateSpring, 2.1f), Sandstone);
                pillar.transform.rotation = rotation;

                // A hanging banner on the inner face of each pillar. Cloth is
                // the cheapest way to break up a wall of grey blocks.
                var banner = CreateBlock("GateBanner",
                    center + side * (s * (GateHalfWidth - 0.1f)) - dir * 0.4f + Vector3.up * (GateSpring * 0.62f),
                    new Vector3(0.42f, GateSpring * 0.8f, 0.08f), new Color(0.62f, 0.16f, 0.15f));
                banner.transform.rotation = rotation;
            }

            BuildArch(center, dir, side, rotation);
            BuildTunnel(center, dir, side, rotation);

            // A dark threshold so the opening reads as a way in, not a hole.
            // Just proud of the sand slab (top at 0) but still under every
            // ground effect, the lowest of which is at 0.04.
            var threshold = CreateBlock("GateFloor", center - dir * 0.4f + Vector3.up * -0.035f,
                new Vector3(GateHalfWidth * 2f, 0.1f, 2.2f), new Color(0.33f, 0.29f, 0.24f));
            threshold.transform.rotation = rotation;
        }

        // A semicircular arch built out of voussoirs — wedge blocks laid
        // radially around the opening — with a keystone at the crown.
        //
        // An earlier pass refused to put anything across the gate at all,
        // because a beam there sits at exactly camera height whenever the
        // player stands in the near gate. The arch is kept and handed to
        // HideWhenBlockingCamera instead, which culls it for the moment it
        // is actually in front of the gorilla.
        void BuildArch(Vector3 center, Vector3 dir, Vector3 side, Quaternion rotation)
        {
            const int voussoirs = 13;
            for (int k = 0; k <= voussoirs; k++)
            {
                float theta = Mathf.PI * k / voussoirs;
                Vector3 radial = side * Mathf.Cos(theta) + Vector3.up * Mathf.Sin(theta);
                Vector3 pos = center + Vector3.up * GateSpring + radial * (GateHalfWidth + 0.28f);

                bool keystone = k == voussoirs / 2;
                var block = CreateBlock(keystone ? "GateKeystone" : "GateVoussoir", pos,
                    new Vector3(0.62f, keystone ? 0.80f : 0.62f, 2.1f),
                    keystone ? StoneLight : (k % 2 == 0 ? Sandstone : Stone));
                // Long axis radial, face pointing out of the arena.
                block.transform.rotation = Quaternion.LookRotation(dir, radial);
                _occluders?.Register(block);
            }
        }

        // The passage the enemies come out of. Side walls and a roof run
        // outward from the opening and are capped by an unlit near-black
        // face, so through the arch you see darkness rather than the grass
        // and sky behind the arena.
        void BuildTunnel(Vector3 center, Vector3 dir, Vector3 side, Quaternion rotation)
        {
            float height = TunnelHeight;

            // Every surface inside the passage is UNLIT and nearly black.
            // Lit materials this dark still catch the key light — the sun
            // comes in over the roof at a shallow enough angle to floodlight
            // the whole tunnel, and the arch framed a brightly lit corridor
            // instead of somewhere deeper in.
            //
            // The jambs are wide enough to close the gap between the bore and
            // the neighbouring stands, so no daylight shows through the side
            // of the gate.
            for (int s = -1; s <= 1; s += 2)
            {
                var wall = CreateUnlitBlock("TunnelWall",
                    center + dir * (TunnelDepth * 0.5f) + side * (s * (GateHalfWidth + 0.75f)) + Vector3.up * (height * 0.5f),
                    new Vector3(1.5f, height, TunnelDepth), new Color(0.085f, 0.080f, 0.095f));
                wall.transform.rotation = rotation;

                // Lit stone on the outer face of each jamb, so from inside
                // the arena the gate is masonry with a dark hole in it.
                var jamb = CreateBlock("GateJamb",
                    center + dir * 0.5f + side * (s * (GateHalfWidth + 0.75f)) + Vector3.up * (height * 0.5f),
                    new Vector3(1.5f, height, 1.2f), s < 0 ? Stone : StoneDark);
                jamb.transform.rotation = rotation;

                // Lit masonry casing wrapping the unlit liner. Without it the
                // passage is a black slab seen from outside the ring, which
                // is very visible on the east and west gates where the
                // stands have tapered away.
                var casing = CreateBlock("TunnelCasing",
                    center + dir * (TunnelDepth * 0.5f) + side * (s * (GateHalfWidth + 1.85f)) + Vector3.up * (height * 0.5f),
                    new Vector3(1.0f, height, TunnelDepth + 1.4f), s < 0 ? StoneDark : Stone);
                casing.transform.rotation = rotation;
            }

            // Back of the structure, closing it off from behind.
            var rear = CreateBlock("TunnelRear",
                center + dir * (TunnelDepth + 0.6f) + Vector3.up * (height * 0.5f),
                new Vector3(GateHalfWidth * 2f + 3.8f, height, 1.2f), StoneDark);
            rear.transform.rotation = rotation;

            // The roof is the one lit piece — it is seen from outside, as the
            // top of the structure, not from within the passage. It is also
            // what the stands above the gate sit on, so it spans the full
            // width of the opening plus both jambs.
            var roof = CreateBlock("TunnelRoof",
                center + dir * (TunnelDepth * 0.5f) + Vector3.up * (height + 0.35f),
                new Vector3(GateHalfWidth * 2f + 3.0f, 0.7f, TunnelDepth + 1.4f), new Color(0.34f, 0.32f, 0.29f));
            roof.transform.rotation = rotation;
            _occluders?.Register(roof);

            // The darkness the passage ends in.
            var back = CreateUnlitBlock("TunnelDark",
                center + dir * TunnelDepth + Vector3.up * (height * 0.5f),
                new Vector3(GateHalfWidth * 2f + 1.2f, height, 0.4f), new Color(0.025f, 0.023f, 0.032f));
            back.transform.rotation = rotation;

            // A floor for the passage, shading from the lit threshold into
            // the dark so enemies walk OUT of shadow rather than appearing.
            var floor = CreateUnlitBlock("TunnelFloor",
                center + dir * (TunnelDepth * 0.5f) + Vector3.up * -0.04f,
                new Vector3(GateHalfWidth * 2f, 0.1f, TunnelDepth), new Color(0.10f, 0.09f, 0.08f));
            floor.transform.rotation = rotation;
        }

        // For the insides of the gate passages, which must stay black under
        // any lighting.
        GameObject CreateUnlitBlock(string name, Vector3 position, Vector3 size, Color color)
        {
            var go = CreateBlock(name, position, size, color);
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialCache.GetUnlit(color);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
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
