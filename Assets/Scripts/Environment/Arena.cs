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

        // A walled well in the middle of the floor. Solid, not a pit — you
        // can't fall in, you just can't walk through it. It exists to break
        // up an otherwise featureless disc: without something at the centre
        // the arena has no landmark and every position in it is the same
        // position.
        public float CoreRadius = 3.2f;

        // CoreRadius is the CENTRELINE of the kerb, not its face: the blocks
        // are 0.7 deep with a 0.95 cap on top, so stone sticks out almost
        // half a metre past it. Clamping to the centreline let everyone walk
        // their front half into the wall.
        public float CoreKeepOutRadius => CoreRadius + 0.5f;

        // Pushes a point OUT of the central well, the mirror of ClampInside.
        public Vector3 ClampOutsideCore(Vector3 position, float margin)
        {
            Vector3 flat = position - Center;
            flat.y = 0f;

            float limit = CoreKeepOutRadius + margin;
            if (flat.sqrMagnitude >= limit * limit) return position;

            // Dead centre: no direction to push along, so pick one.
            if (flat.sqrMagnitude < 0.0001f) flat = Vector3.forward;

            Vector3 pushed = Center + flat.normalized * limit;
            pushed.y = position.y;
            return pushed;
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
            BuildCore();
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
                // Tiled barely more than once across the whole floor, so the
                // repeat is invisible: at the old Radius/3 the same square
                // of sand appeared ten times and the grid was obvious.
                MaterialCache.GetTextured(ProceduralTextures.Sand(), Color.white, new Vector2(1.35f, 1.35f)));
        }

        // The central well: a low stone kerb ringing a black void. The void
        // is unlit and reads as bottomless, but nothing ever enters it —
        // players and enemies are both pushed out at the kerb.
        void BuildCore()
        {
            const int segments = 28;
            const float kerbHeight = 0.75f;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * 360f / segments;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                var facing = Quaternion.LookRotation(dir, Vector3.up);

                var block = CreateBlock("CoreKerb" + i,
                    Center + dir * CoreRadius + Vector3.up * (kerbHeight * 0.5f),
                    new Vector3(0.95f, kerbHeight, 0.7f), i % 2 == 0 ? Stone : StoneDark);
                block.transform.rotation = facing;

                var cap = CreateBlock("CoreCap" + i,
                    Center + dir * CoreRadius + Vector3.up * (kerbHeight + 0.09f),
                    new Vector3(1.0f, 0.18f, 0.95f), StoneLight);
                cap.transform.rotation = facing;
            }

            // The dark inside. Sunk a little so the kerb reads as a rim
            // around it rather than a ring sitting on the floor.
            var pit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pit.name = "CoreVoid";
            Destroy(pit.GetComponent<Collider>());
            pit.transform.SetParent(transform, false);
            // Just ABOVE the sand slab, whose top is y=0. Sinking it below
            // the slab simply hid it — the floor is solid and drew over it,
            // so the well read as a stone ring around more sand.
            pit.transform.position = Center + Vector3.up * 0.03f;
            pit.transform.localScale = new Vector3(CoreRadius * 2f, 0.02f, CoreRadius * 2f);
            var mr = pit.GetComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialCache.GetUnlit(new Color(0.02f, 0.02f, 0.028f));
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
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

                // The low wall stays SOLID all the way round, including the
                // near arc: it is short enough never to hide the gorilla, and
                // it is what keeps the ring reading as an enclosure rather
                // than a floating floor.
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
        // Tiered stands stepping up and outward behind the wall.
        //
        // The ring is FULL HEIGHT the whole way round, including the southern
        // arc the fixed camera looks in over.
        //
        // That arc is the hard case: the camera sits south of the player and
        // outside the wall, so those stands are between it and the fight.
        // Two earlier answers both failed — tapering them to nothing made the
        // colosseum visibly stop being a colosseum at the bottom of the
        // screen, and rendering them translucent was worse still, because the
        // camera is physically INSIDE them and five stacked layers of glass
        // hid the gorilla completely.
        //
        // So they are built solid and handed to HideWhenBlockingCamera, which
        // culls a piece only while it actually sits between the camera and
        // the player. Stand anywhere but the south wall and the ring is
        // whole; walk up to it and the few blocks in front of you get out of
        // the way.
        void BuildStands(int index, Vector3 dir, Vector3 basePos, Quaternion facing, float baseY = 0f)
        {
            // Only the segments the camera genuinely looks THROUGH may hide.
            //
            // This was < 0.35, which is three quarters of the ring — every
            // stand that wasn't pointing away from the camera was allowed to
            // cull itself, so standing in a corner made spectators off to
            // the side vanish while they were nowhere near the fight. The
            // band is now the southern arc only.
            float northness = Vector3.Dot(dir, Vector3.forward);
            bool near = northness < -0.6f;

            basePos += Vector3.up * baseY;

            // Five tiers: a colosseum's whole character is that it keeps
            // going up.
            const int tiers = 5;
            for (int t = 0; t < tiers; t++)
            {
                float outward = 1.5f + t * 1.5f;
                float height = WallHeight + 0.8f + t * 1.5f;

                var tier = CreateBlock("Tier" + index + "_" + t,
                    basePos + dir * outward + Vector3.up * (height * 0.5f),
                    new Vector3(1.8f, height, 1.6f),
                    (index + t) % 2 == 0 ? Stone : StoneDark);
                tier.transform.rotation = facing;
                if (near) _occluders?.Register(tier);

                // A crowd watching the fight. Small cubes in a row are enough
                // at this distance, and they do more for "colosseum" than any
                // amount of extra stonework. Near-arc rows are registered
                // with the occluder alongside the tier they stand on, so a
                // culled tier doesn't leave its spectators floating.
                if (index % 2 == 0)
                {
                    var row = BuildCrowdRow(basePos + dir * outward, dir, height, facing);
                    if (near) foreach (var person in row) _occluders?.Register(person);
                }
            }
        }

        static readonly Color[] CrowdTones =
        {
            new Color(0.78f, 0.30f, 0.26f), new Color(0.30f, 0.38f, 0.66f),
            new Color(0.82f, 0.72f, 0.40f), new Color(0.34f, 0.56f, 0.36f),
            new Color(0.60f, 0.40f, 0.62f), new Color(0.85f, 0.82f, 0.76f),
        };

        readonly System.Collections.Generic.List<GameObject> _crowdScratch = new System.Collections.Generic.List<GameObject>();

        System.Collections.Generic.List<GameObject> BuildCrowdRow(Vector3 tierPos, Vector3 dir, float tierHeight, Quaternion facing)
        {
            _crowdScratch.Clear();
            Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
            for (int s = -1; s <= 1; s++)
            {
                var person = CreateBlock("Crowd", tierPos + side * (s * 0.55f) + Vector3.up * (tierHeight + 0.22f),
                    new Vector3(0.28f, 0.44f, 0.28f), CrowdTones[Random.Range(0, CrowdTones.Length)]);
                person.transform.rotation = facing;
                var head = CreateBlock("CrowdHead", tierPos + side * (s * 0.55f) + Vector3.up * (tierHeight + 0.54f),
                    Vector3.one * 0.2f, new Color(0.72f, 0.56f, 0.42f));
                head.transform.rotation = facing;
                _crowdScratch.Add(person);
                _crowdScratch.Add(head);
            }
            return _crowdScratch;
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

        // Which pieces are ALLOWED to hide themselves.
        //
        // This is an explicit opt-in rather than a rule the occluder works
        // out for itself, because no distance test gets it right: the bits
        // that genuinely stand between the camera and the fight are the
        // southern gate's superstructure and the tier directly behind it,
        // and everything else that qualified under a radius test — the side
        // stands, their crowds — was only ever disappearing by accident.
        bool _canHide;

        // The gate the camera looks straight down. The others are seen edge
        // on or from behind and never cover the gorilla.
        static bool IsSouth(Vector3 dir) => Vector3.Dot(dir, Vector3.back) > 0.7f;

        void RegisterOccluder(GameObject piece)
        {
            if (!_canHide || piece == null) return;
            _occluders?.Register(piece);
        }

        void BuildGate(Vector3 dir)
        {
            Vector3 center = Center + dir * Radius;
            Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
            var rotation = Quaternion.LookRotation(dir, Vector3.up);

            _canHide = IsSouth(dir);

            for (int s = -1; s <= 1; s += 2)
            {
                // Directly under the arch's springing (see BuildArch), so the
                // arch lands on the pillars instead of floating past them.
                Vector3 jamb = center + side * (s * (GateHalfWidth + 0.28f));

                // Plinth, fluted shaft, capital: a real column rather than a
                // single slab. Both sides take the SAME colours — the jambs
                // and casings used to alternate stone/dark by side, which
                // made every gate visibly lopsided.
                var plinth = CreateBlock("GatePlinth", jamb + Vector3.up * 0.17f,
                    new Vector3(1.4f, 0.34f, 2.4f), StoneDark);
                plinth.transform.rotation = rotation;

                var pillar = CreateBlock("GatePillar", jamb + Vector3.up * (GateSpring * 0.5f + 0.17f),
                    new Vector3(1.1f, GateSpring, 2.1f), Sandstone);
                pillar.transform.rotation = rotation;

                // Three shallow flutes down the inner face of each column.
                for (int f = -1; f <= 1; f++)
                {
                    var flute = CreateBlock("GateFlute",
                        jamb + side * (s * -0.46f) + dir * (f * 0.6f) + Vector3.up * (GateSpring * 0.5f + 0.17f),
                        new Vector3(0.22f, GateSpring * 0.88f, 0.22f), StoneLight);
                    flute.transform.rotation = rotation;
                }

                var capital = CreateBlock("GateCapital", jamb + Vector3.up * (GateSpring + 0.3f),
                    new Vector3(1.45f, 0.28f, 2.45f), StoneLight);
                capital.transform.rotation = rotation;

                // A hanging banner on the inner face of each pillar. Cloth is
                // the cheapest way to break up a wall of grey blocks.
                var banner = CreateBlock("GateBanner",
                    center + side * (s * (GateHalfWidth - 0.1f)) - dir * 0.4f + Vector3.up * (GateSpring * 0.62f),
                    new Vector3(0.42f, GateSpring * 0.8f, 0.08f), new Color(0.62f, 0.16f, 0.15f));
                banner.transform.rotation = rotation;

                var bannerHem = CreateBlock("GateBannerHem",
                    center + side * (s * (GateHalfWidth - 0.1f)) - dir * 0.4f + Vector3.up * (GateSpring * 0.23f),
                    new Vector3(0.42f, 0.14f, 0.085f), new Color(0.80f, 0.64f, 0.24f));
                bannerHem.transform.rotation = rotation;

                // A finial standing on each capital, above the arch line.
                var finial = CreateBlock("GateFinial", jamb + Vector3.up * (GateStructureTop + 0.32f),
                    new Vector3(0.52f, 0.64f, 0.52f), Sandstone);
                finial.transform.rotation = rotation;
                var finialCap = CreateBlock("GateFinialCap", jamb + Vector3.up * (GateStructureTop + 0.76f),
                    Vector3.one * 0.36f, StoneLight);
                finialCap.transform.rotation = rotation;
                RegisterOccluder(finial);
                RegisterOccluder(finialCap);
            }

            // A cornice course across the top of the arch, tying the two
            // columns into one facade.
            var cornice = CreateBlock("GateCornice",
                center + Vector3.up * (GateStructureTop - 0.2f),
                new Vector3(GateHalfWidth * 2f + 2.6f, 0.38f, 2.6f), StoneLight);
            cornice.transform.rotation = rotation;
            RegisterOccluder(cornice);

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
            // EVEN number of steps, so k = voussoirs/2 lands at exactly 90
            // degrees and the keystone actually sits at the crown. With an
            // odd count the "keystone" was a block at 83 degrees — visibly
            // off to one side of the top of every arch.
            const int voussoirs = 12;
            for (int k = 0; k <= voussoirs; k++)
            {
                float theta = Mathf.PI * k / voussoirs;
                Vector3 radial = side * Mathf.Cos(theta) + Vector3.up * Mathf.Sin(theta);
                Vector3 pos = center + Vector3.up * GateSpring + radial * (GateHalfWidth + 0.28f);

                bool keystone = k == voussoirs / 2;
                var block = CreateBlock(keystone ? "GateKeystone" : "GateVoussoir", pos,
                    new Vector3(0.62f, keystone ? 0.86f : 0.62f, 2.2f),
                    keystone ? StoneLight : (k % 2 == 0 ? Sandstone : Stone));
                // Long axis radial, face pointing out of the arena.
                block.transform.rotation = Quaternion.LookRotation(dir, radial);
                RegisterOccluder(block);

                // An archivolt: a second, thinner ring of stone outside the
                // first, which is what gives a real arch its depth.
                var outer = CreateBlock("GateArchivolt",
                    center + Vector3.up * GateSpring + radial * (GateHalfWidth + 0.82f),
                    new Vector3(0.58f, 0.40f, 1.5f), k % 2 == 0 ? Stone : StoneDark);
                outer.transform.rotation = Quaternion.LookRotation(dir, radial);
                RegisterOccluder(outer);
            }

            // The keystone proud of the ring, and a boss on its face.
            Vector3 crown = center + Vector3.up * (GateSpring + GateHalfWidth + 0.28f);
            var crownBlock = CreateBlock("GateKeystoneCap", crown + Vector3.up * 0.42f,
                new Vector3(0.78f, 0.44f, 2.4f), StoneLight);
            crownBlock.transform.rotation = rotation;
            RegisterOccluder(crownBlock);

            var boss = CreateBlock("GateBoss", crown - dir * 1.15f + Vector3.up * 0.1f,
                new Vector3(0.44f, 0.44f, 0.22f), Sandstone);
            boss.transform.rotation = rotation;
            RegisterOccluder(boss);
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
                // NOTHING here may share a face plane with anything else.
                //
                // The black liner and the stone jamb used to sit at the same
                // lateral centre, at the same width, at the same height, and
                // they overlap in depth — so their side faces and their tops
                // were exactly coplanar, near-black against near-white,
                // fighting for identical pixels. That is the flicker at the
                // gates, and it was worst at the south one because that is
                // the gate the camera looks straight down.
                //
                // The liner is the narrower, shorter piece and the masonry
                // encloses it, so every shared surface now has a clear
                // winner at every depth.
                var wall = CreateUnlitBlock("TunnelWall",
                    center + dir * (TunnelDepth * 0.5f) + side * (s * (GateHalfWidth + 0.72f)) + Vector3.up * (height * 0.47f),
                    new Vector3(1.38f, height * 0.94f, TunnelDepth), new Color(0.085f, 0.080f, 0.095f));
                wall.transform.rotation = rotation;

                // Lit stone on the outer face of each jamb, so from inside
                // the arena the gate is masonry with a dark hole in it.
                var jamb = CreateBlock("GateJamb",
                    center + dir * 0.5f + side * (s * (GateHalfWidth + 0.78f)) + Vector3.up * (height * 0.5f),
                    new Vector3(1.72f, height, 1.2f), Stone);
                jamb.transform.rotation = rotation;

                // Lit masonry casing wrapping the unlit liner. Without it the
                // passage is a black slab seen from outside the ring, which
                // is very visible on the east and west gates where the
                // stands have tapered away.
                var casing = CreateBlock("TunnelCasing",
                    center + dir * (TunnelDepth * 0.5f) + side * (s * (GateHalfWidth + 1.9f)) + Vector3.up * (height * 0.52f),
                    new Vector3(1.0f, height * 1.04f, TunnelDepth + 1.4f), StoneDark);
                casing.transform.rotation = rotation;
            }

            // Back of the structure, closing it off from behind. Its own
            // height again, for the same reason as everything above.
            var rear = CreateBlock("TunnelRear",
                center + dir * (TunnelDepth + 0.6f) + Vector3.up * (height * 0.51f),
                new Vector3(GateHalfWidth * 2f + 3.8f, height * 1.02f, 1.2f), StoneDark);
            rear.transform.rotation = rotation;

            // The roof is the one lit piece — it is seen from outside, as the
            // top of the structure, not from within the passage. It is also
            // what the stands above the gate sit on, so it spans the full
            // width of the opening plus both jambs.
            var roof = CreateBlock("TunnelRoof",
                center + dir * (TunnelDepth * 0.5f) + Vector3.up * (height + 0.35f),
                new Vector3(GateHalfWidth * 2f + 3.0f, 0.7f, TunnelDepth + 1.4f), new Color(0.34f, 0.32f, 0.29f));
            roof.transform.rotation = rotation;
            RegisterOccluder(roof);

            // The darkness the passage ends in.
            var back = CreateUnlitBlock("TunnelDark",
                center + dir * TunnelDepth + Vector3.up * (height * 0.5f),
                new Vector3(GateHalfWidth * 2f + 1.2f, height, 0.4f), new Color(0.025f, 0.023f, 0.032f));
            back.transform.rotation = rotation;

            // A floor for the passage, shading from the lit threshold into
            // the dark so enemies walk OUT of shadow rather than appearing.
            // Its top sat 1cm above the sand slab, which at this distance
            // from the camera is well inside what the depth buffer can
            // resolve — the two surfaces traded places as the camera moved
            // and the threshold strobed black against the sand. 5cm is
            // still an invisible step and is comfortably clear of it.
            var floor = CreateUnlitBlock("TunnelFloor",
                center + dir * (TunnelDepth * 0.5f) + Vector3.up * 0f,
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
