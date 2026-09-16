using System;
using System.Collections.Generic;
using UnityEngine;
using GorillaSurvivors.Core;
using GorillaSurvivors.Player;

namespace GorillaSurvivors.Environment
{
    // Keeps the world dressed around the player forever. Props used to be
    // scattered once at startup inside a fixed radius, so travelling far
    // enough in any direction left the player on an empty plane. This keeps
    // a target population within a radius of the player instead: anything
    // that falls too far behind is removed and respawned off-camera ahead,
    // so the count (and the cost) stays constant no matter how far you roam.
    public class EnvironmentStreamer : MonoBehaviour
    {
        // Populated out to KeepRadius; culled past CullRadius. The gap
        // between them is hysteresis — without it, props on the boundary
        // would thrash between spawned and culled as the player jitters.
        public float KeepRadius = 55f;
        public float CullRadius = 80f;
        public float MaintenanceInterval = 0.3f;
        public int SpawnBudgetPerPass = 10;

        class PropKind
        {
            public string Name;
            public int Target;
            public Func<GameObject> Create;
            public float MinScale = 1f;
            public float MaxScale = 1f;
            public bool RandomYaw = true;
            // Lush undergrowth belongs on the grass outside; the arena floor
            // is raked sand, and carpeting it in flowers and ferns undoes
            // that read. Props that are gameplay (trees to fell, rocks to
            // launch) stay regardless.
            // How many to keep inside an arena. The streaming targets above
            // were sized for an endless map and scaling them by area gives
            // one or two of everything, which is too sparse for props the
            // player is meant to use; 0 means "grass only, keep it out".
            public int ArenaTarget;
            public bool AllowedInArena => ArenaTarget > 0;
            // Optional arena-specific size range; 0 means "use MinScale and
            // MaxScale". Columns want to be smaller inside the ring than a
            // tree does out on the grass.
            public float ArenaMinScale;
            public float ArenaMaxScale;
            // Props mid-use (a launched boulder, a toppling tree) must never
            // be culled out from under the effect that is driving them.
            public Func<GameObject, bool> IsBusy;
            public readonly List<GameObject> Live = new List<GameObject>();
        }

        readonly List<PropKind> _kinds = new List<PropKind>();
        float _nextMaintenance;

        void Awake()
        {
            _kinds.Add(new PropKind
            {
                Name = "Tree",
                Target = 26,
                ArenaTarget = 5,
                MinScale = 0.85f,
                MaxScale = 1.3f,
                ArenaMinScale = 0.72f,
                ArenaMaxScale = 0.95f,
                // Inside the arena these are stone columns, not trees. A
                // forest growing out of a colosseum floor never made sense;
                // a column does the same job — cover to break line of sight,
                // and something heavy to topple onto a crowd — and belongs.
                Create = () =>
                {
                    bool inArena = Arena.Instance != null;
                    var t = inArena ? Blocky3DArt.Column() : Blocky3DArt.Tree();
                    var fellable = t.AddComponent<FellableTree>();
                    fellable.RemnantIsRubble = inArena;
                    return t;
                },
                IsBusy = go => { var f = go.GetComponent<FellableTree>(); return f != null && f.IsFelled; },
            });

            _kinds.Add(new PropKind
            {
                Name = "Rock",
                Target = 24,
                ArenaTarget = 8,
                MinScale = 0.8f,
                MaxScale = 1.2f,
                Create = () => { var r = Blocky3DArt.Rock(); r.AddComponent<AttackableRock>(); return r; },
                IsBusy = go => { var rock = go.GetComponent<AttackableRock>(); return rock != null && rock.IsLaunched; },
            });

            _kinds.Add(new PropKind { Name = "Bush", Target = 30, MinScale = 0.8f, MaxScale = 1.3f, Create = Blocky3DArt.Bush });
            _kinds.Add(new PropKind { Name = "GrassTuft", Target = 220, MinScale = 0.8f, MaxScale = 1.5f, Create = Blocky3DArt.GrassTuft });
            _kinds.Add(new PropKind { Name = "Flower", Target = 60, MinScale = 0.85f, MaxScale = 1.25f, Create = Blocky3DArt.Flower });
            _kinds.Add(new PropKind { Name = "Mushroom", Target = 24, MinScale = 0.8f, MaxScale = 1.4f, Create = Blocky3DArt.Mushroom });
            // Stumps and logs are forest litter; inside the colosseum the
            // equivalent scatter is broken masonry, which the Rubble kind
            // below supplies.
            _kinds.Add(new PropKind { Name = "Stump", Target = 10, Create = Blocky3DArt.Stump });
            _kinds.Add(new PropKind { Name = "Log", Target = 12, MinScale = 0.85f, MaxScale = 1.25f, Create = Blocky3DArt.Log });
            _kinds.Add(new PropKind { Name = "Rubble", Target = 0, ArenaTarget = 5, MinScale = 0.8f, MaxScale = 1.2f, Create = Blocky3DArt.ColumnRubble });
            _kinds.Add(new PropKind { Name = "Fern", Target = 45, MinScale = 0.85f, MaxScale = 1.35f, Create = Blocky3DArt.Fern });
            _kinds.Add(new PropKind { Name = "Pebbles", Target = 40, ArenaTarget = 12, MinScale = 0.8f, MaxScale = 1.4f, Create = Blocky3DArt.Pebbles });
        }

        // Fills the starting area. Unlike the streaming passes this ignores
        // the off-camera rule, because at startup the player is looking at
        // ground that has to already be dressed.
        public void PopulateInitial()
        {
            // Inside an arena the whole playfield is populated at once and
            // the per-kind targets are scaled to its area — the streaming
            // radius was sized for an endless map, and reusing it would pack
            // an arena-sized space with several times the intended clutter.
            var arena = Arena.Instance;
            if (arena != null)
            {
                foreach (var kind in _kinds)
                {
                    kind.Target = kind.ArenaTarget;
                    for (int i = 0; i < kind.Target; i++) Place(kind, RandomArenaPoint(arena));
                }
                return;
            }

            Vector3 origin = PlayerPosition();
            foreach (var kind in _kinds)
            {
                for (int i = 0; i < kind.Target; i++)
                {
                    var dir = UnityEngine.Random.insideUnitCircle.normalized;
                    float dist = UnityEngine.Random.Range(4f, KeepRadius);
                    Place(kind, origin + new Vector3(dir.x * dist, 0f, dir.y * dist));
                }
            }
        }

        // Refills prefer somewhere off camera so props don't blink into
        // existence in front of the player, but in a small arena that isn't
        // always possible — fall back to anywhere inside rather than stall.
        static Vector3 PickArenaRefillPoint(Arena arena)
        {
            var cam = Camera.main;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector3 candidate = RandomArenaPoint(arena);
                if (cam == null) return candidate;

                Vector3 viewport = cam.WorldToViewportPoint(candidate);
                bool onScreen = viewport.z > 0f
                    && viewport.x > -0.05f && viewport.x < 1.05f
                    && viewport.y > -0.05f && viewport.y < 1.05f;
                if (!onScreen) return candidate;
            }
            return RandomArenaPoint(arena);
        }

        static Vector3 RandomArenaPoint(Arena arena)
        {
            // sqrt on the radius keeps the scatter even instead of bunching
            // everything toward the middle — here it maps onto an ANNULUS,
            // because the middle of the arena is the well. Rocks and columns
            // landing on it hung over a black void, in the one part of the
            // floor nobody can reach.
            float outer = Mathf.Max(1f, arena.Radius - 2.5f);
            float inner = Mathf.Min(arena.CoreKeepOutRadius + 1.2f, outer - 0.5f);

            var dir = UnityEngine.Random.insideUnitCircle.normalized;
            float t = UnityEngine.Random.Range(inner * inner / (outer * outer), 1f);
            float dist = Mathf.Sqrt(t) * outer;
            return arena.Center + new Vector3(dir.x * dist, 0f, dir.y * dist);
        }

        void Update()
        {
            if (Time.time < _nextMaintenance) return;
            _nextMaintenance = Time.time + MaintenanceInterval;

            var arena = Arena.Instance;
            Vector3 origin = PlayerPosition();
            int budget = SpawnBudgetPerPass;

            foreach (var kind in _kinds)
            {
                // Drop anything destroyed elsewhere (a felled tree cleaning
                // itself up, a boulder that exploded).
                kind.Live.RemoveAll(go => go == null);

                // Culling only applies to an endless map. In an arena nothing
                // is ever far away, and the refill below just replaces props
                // the player consumed.
                if (arena == null)
                {
                    for (int i = kind.Live.Count - 1; i >= 0; i--)
                    {
                        var go = kind.Live[i];
                        if (kind.IsBusy != null && kind.IsBusy(go)) continue;
                        if (Vector3.Distance(go.transform.position, origin) <= CullRadius) continue;

                        Destroy(go);
                        kind.Live.RemoveAt(i);
                    }
                }

                while (kind.Live.Count < kind.Target && budget > 0)
                {
                    budget--;
                    Place(kind, arena != null ? PickArenaRefillPoint(arena) : WorldScatter.OffscreenPosition(18f, KeepRadius));
                }
            }
        }

        void Place(PropKind kind, Vector3 position)
        {
            var go = kind.Create();
            go.transform.position = position;
            if (kind.RandomYaw) go.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            bool useArenaScale = Arena.Instance != null && kind.ArenaMaxScale > 0f;
            float min = useArenaScale ? kind.ArenaMinScale : kind.MinScale;
            float max = useArenaScale ? kind.ArenaMaxScale : kind.MaxScale;
            go.transform.localScale = Vector3.one * UnityEngine.Random.Range(min, max);
            kind.Live.Add(go);
        }

        static Vector3 PlayerPosition()
        {
            return PlayerController.Instance != null ? PlayerController.Instance.transform.position : Vector3.zero;
        }
    }
}
