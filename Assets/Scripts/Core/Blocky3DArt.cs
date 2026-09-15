using UnityEngine;

namespace GorillaSurvivors.Core
{
    public enum HumanVariant { Grunt, Runner, Brute, Thrower }

    // Simple primitive-assembled 3D models (spheres/capsules/cubes), the 3D
    // counterpart to CreatureArt's procedural 2D sprites. No textures/lighting
    // needed — parts use unlit colored materials via MaterialCache.
    public static class Blocky3DArt
    {
        public static GameObject Gorilla()
        {
            var root = new GameObject("GorillaModel");

            var fur = new Color(0.32f, 0.21f, 0.15f);
            var furDark = new Color(0.16f, 0.11f, 0.09f);
            var skin = new Color(0.60f, 0.45f, 0.36f);

            AddPrimitive(root.transform, "Body", PrimitiveType.Sphere, new Vector3(0, 0.55f, 0), new Vector3(0.95f, 0.85f, 0.8f), fur);
            AddPrimitive(root.transform, "Chest", PrimitiveType.Sphere, new Vector3(0, 0.62f, 0.32f), new Vector3(0.55f, 0.5f, 0.22f), skin);
            AddPrimitive(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0, 1.15f, 0.05f), Vector3.one * 0.55f, fur);
            AddPrimitive(root.transform, "Brow", PrimitiveType.Sphere, new Vector3(0, 1.22f, 0.32f), new Vector3(0.38f, 0.14f, 0.16f), furDark);
            AddPrimitive(root.transform, "EarL", PrimitiveType.Sphere, new Vector3(-0.28f, 1.32f, 0f), Vector3.one * 0.16f, furDark);
            AddPrimitive(root.transform, "EarR", PrimitiveType.Sphere, new Vector3(0.28f, 1.32f, 0f), Vector3.one * 0.16f, furDark);
            AddPrimitive(root.transform, "Face", PrimitiveType.Sphere, new Vector3(0, 1.05f, 0.28f), new Vector3(0.32f, 0.28f, 0.2f), skin);
            AddPrimitive(root.transform, "Snout", PrimitiveType.Sphere, new Vector3(0, 0.98f, 0.38f), new Vector3(0.24f, 0.16f, 0.16f), skin);
            AddPrimitive(root.transform, "EyeL", PrimitiveType.Sphere, new Vector3(-0.1f, 1.12f, 0.32f), Vector3.one * 0.06f, Color.black);
            AddPrimitive(root.transform, "EyeR", PrimitiveType.Sphere, new Vector3(0.1f, 1.12f, 0.32f), Vector3.one * 0.06f, Color.black);
            AddPrimitive(root.transform, "NostrilL", PrimitiveType.Sphere, new Vector3(-0.05f, 0.96f, 0.46f), Vector3.one * 0.03f, furDark);
            AddPrimitive(root.transform, "NostrilR", PrimitiveType.Sphere, new Vector3(0.05f, 0.96f, 0.46f), Vector3.one * 0.03f, furDark);
            AddArm(root.transform, "ArmL", new Vector3(-0.62f, 0.82f, 0f), 0.15f, 0.55f, fur);
            AddArm(root.transform, "ArmR", new Vector3(0.62f, 0.82f, 0f), 0.15f, 0.55f, fur);
            AddCapsule(root.transform, "LegL", new Vector3(-0.32f, 0.12f, 0f), 0.16f, 0.3f, fur);
            AddCapsule(root.transform, "LegR", new Vector3(0.32f, 0.12f, 0f), 0.16f, 0.3f, fur);

            return root;
        }

        public static GameObject Human(HumanVariant variant = HumanVariant.Grunt, EnemyModifierRoll modifiers = default)
        {
            var root = new GameObject("HumanModel");

            var skin = new Color(0.85f, 0.68f, 0.58f);
            var pants = new Color(0.30f, 0.30f, 0.36f);
            var hair = new Color(0.20f, 0.15f, 0.12f);
            var shoe = new Color(0.10f, 0.10f, 0.10f);
            var belt = new Color(0.15f, 0.12f, 0.10f);

            Color shirt;
            float scale;
            bool holdsWeapon = false;
            switch (variant)
            {
                case HumanVariant.Runner:
                    shirt = new Color(0.30f, 0.78f, 0.38f);
                    scale = 0.85f;
                    break;
                case HumanVariant.Brute:
                    shirt = new Color(0.38f, 0.16f, 0.48f);
                    scale = 1.55f;
                    break;
                case HumanVariant.Thrower:
                    shirt = new Color(0.88f, 0.56f, 0.16f);
                    scale = 1f;
                    holdsWeapon = true;
                    break;
                default:
                    shirt = new Color(0.85f, 0.22f, 0.20f);
                    scale = 1f;
                    break;
            }

            // A weapon-tier modifier reuses the Thrower's held-weapon prop
            // (recoloring it if it already had one) instead of stacking a
            // second prop in the same hand.
            var weaponColor = new Color(0.35f, 0.25f, 0.15f);
            if (modifiers.Weapon != ModifierTier.None)
            {
                holdsWeapon = true;
                weaponColor = EnemyModifierRoll.TierColor(modifiers.Weapon);
            }

            AddCapsule(root.transform, "LegL", new Vector3(-0.13f, 0.32f, 0f), 0.11f, 0.6f, pants);
            AddCapsule(root.transform, "LegR", new Vector3(0.13f, 0.32f, 0f), 0.11f, 0.6f, pants);
            AddPrimitive(root.transform, "ShoeL", PrimitiveType.Sphere, new Vector3(-0.13f, 0.05f, 0.05f), new Vector3(0.16f, 0.1f, 0.22f), shoe);
            AddPrimitive(root.transform, "ShoeR", PrimitiveType.Sphere, new Vector3(0.13f, 0.05f, 0.05f), new Vector3(0.16f, 0.1f, 0.22f), shoe);
            AddArm(root.transform, "ArmL", new Vector3(-0.32f, 1.06f, 0f), 0.09f, 0.55f, skin);
            AddArm(root.transform, "ArmR", new Vector3(0.32f, 1.06f, 0f), 0.09f, 0.55f, skin);
            AddPrimitive(root.transform, "Torso", PrimitiveType.Capsule, new Vector3(0, 0.85f, 0), new Vector3(0.5f, 0.4f, 0.32f), shirt);
            AddPrimitive(root.transform, "Belt", PrimitiveType.Cube, new Vector3(0, 0.68f, 0), new Vector3(0.52f, 0.08f, 0.34f), belt);
            AddPrimitive(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0, 1.42f, 0), Vector3.one * 0.34f, skin);
            AddPrimitive(root.transform, "Hair", PrimitiveType.Sphere, new Vector3(0, 1.52f, -0.02f), new Vector3(0.36f, 0.24f, 0.36f), hair);
            AddPrimitive(root.transform, "EyeL", PrimitiveType.Sphere, new Vector3(-0.09f, 1.40f, 0.15f), Vector3.one * 0.045f, Color.black);
            AddPrimitive(root.transform, "EyeR", PrimitiveType.Sphere, new Vector3(0.09f, 1.40f, 0.15f), Vector3.one * 0.045f, Color.black);

            if (holdsWeapon)
            {
                var weapon = AddPrimitive(root.transform, "Weapon", PrimitiveType.Capsule, new Vector3(0.42f, 0.65f, 0.15f), new Vector3(0.08f, 0.4f, 0.08f), weaponColor);
                weapon.transform.localRotation = Quaternion.Euler(60f, 0f, 20f);
            }

            if (modifiers.Armor != ModifierTier.None)
            {
                AddPrimitive(root.transform, "Armor", PrimitiveType.Cube, new Vector3(0, 0.85f, 0.06f), new Vector3(0.56f, 0.42f, 0.36f), EnemyModifierRoll.TierColor(modifiers.Armor));
            }

            if (modifiers.HasShoes)
            {
                var glow = new Color(0.85f, 0.9f, 0.2f);
                AddPrimitive(root.transform, "ShoeGlowL", PrimitiveType.Sphere, new Vector3(-0.13f, 0.05f, 0.08f), new Vector3(0.20f, 0.12f, 0.28f), glow);
                AddPrimitive(root.transform, "ShoeGlowR", PrimitiveType.Sphere, new Vector3(0.13f, 0.05f, 0.08f), new Vector3(0.20f, 0.12f, 0.28f), glow);
            }

            if (modifiers.HasCrown)
            {
                var gold = new Color(1f, 0.85f, 0.2f);
                AddPrimitive(root.transform, "Crown", PrimitiveType.Cylinder, new Vector3(0, 1.63f, 0), new Vector3(0.24f, 0.07f, 0.24f), gold);
                var spike = AddPrimitive(root.transform, "CrownSpike", PrimitiveType.Cube, new Vector3(0, 1.72f, 0), new Vector3(0.06f, 0.1f, 0.06f), gold);
                spike.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            }

            root.transform.localScale = Vector3.one * scale;
            return root;
        }

        public static GameObject Gem()
        {
            var root = new GameObject("GemModel");
            var gold = new Color(0.98f, 0.82f, 0.18f);
            var cube = AddPrimitive(root.transform, "Diamond", PrimitiveType.Cube, new Vector3(0, 0.3f, 0), new Vector3(0.28f, 0.28f, 0.28f), gold);
            cube.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            return root;
        }

        public static GameObject Banana()
        {
            var root = new GameObject("BananaModel");
            var yellow = new Color(0.95f, 0.82f, 0.15f);
            var yellowDark = new Color(0.85f, 0.70f, 0.12f);
            var tip = new Color(0.45f, 0.35f, 0.12f);

            AddOneBanana(root.transform, new Vector3(-0.08f, 0f, 0.05f), 20f, yellow, tip);
            AddOneBanana(root.transform, new Vector3(0.1f, 0.05f, -0.05f), -25f, yellowDark, tip);
            AddGlowRing(root.transform, yellow);

            return root;
        }

        static void AddOneBanana(Transform parent, Vector3 offset, float tiltZ, Color color, Color tip)
        {
            var group = new GameObject("Banana");
            group.transform.SetParent(parent, false);
            group.transform.localPosition = offset;

            var body = AddCapsule(group.transform, "Body", new Vector3(0, 0.35f, 0), 0.11f, 0.5f, color);
            body.transform.localRotation = Quaternion.Euler(0f, 0f, 35f + tiltZ);
            AddPrimitive(group.transform, "TipA", PrimitiveType.Sphere, new Vector3(-0.18f, 0.58f, 0f), Vector3.one * 0.07f, tip);
            AddPrimitive(group.transform, "TipB", PrimitiveType.Sphere, new Vector3(0.18f, 0.12f, 0f), Vector3.one * 0.06f, tip);
        }

        public static GameObject Adrenaline()
        {
            var root = new GameObject("AdrenalineModel");
            var cyan = new Color(0.25f, 0.85f, 0.95f);
            var cyanLight = new Color(0.55f, 0.95f, 1f);

            var big = AddPrimitive(root.transform, "ShardMain", PrimitiveType.Cube, new Vector3(0, 0.42f, 0), new Vector3(0.2f, 0.5f, 0.2f), cyan);
            big.transform.localRotation = Quaternion.Euler(0f, 45f, 45f);
            var s1 = AddPrimitive(root.transform, "ShardA", PrimitiveType.Cube, new Vector3(0.16f, 0.22f, 0.05f), new Vector3(0.13f, 0.32f, 0.13f), cyanLight);
            s1.transform.localRotation = Quaternion.Euler(15f, 20f, 30f);
            var s2 = AddPrimitive(root.transform, "ShardB", PrimitiveType.Cube, new Vector3(-0.15f, 0.18f, -0.08f), new Vector3(0.11f, 0.26f, 0.11f), cyan);
            s2.transform.localRotation = Quaternion.Euler(-10f, -25f, 60f);
            AddGlowRing(root.transform, cyan);
            return root;
        }

        public static GameObject Rampage()
        {
            var root = new GameObject("RampageModel");
            var magenta = new Color(0.85f, 0.20f, 0.75f);
            var dark = new Color(0.55f, 0.10f, 0.48f);

            AddPrimitive(root.transform, "Fist", PrimitiveType.Sphere, new Vector3(0, 0.4f, 0), Vector3.one * 0.4f, magenta);
            AddPrimitive(root.transform, "KnuckleL", PrimitiveType.Sphere, new Vector3(-0.14f, 0.58f, 0.1f), Vector3.one * 0.15f, magenta);
            AddPrimitive(root.transform, "KnuckleM", PrimitiveType.Sphere, new Vector3(0f, 0.62f, 0.1f), Vector3.one * 0.15f, magenta);
            AddPrimitive(root.transform, "KnuckleR", PrimitiveType.Sphere, new Vector3(0.14f, 0.58f, 0.1f), Vector3.one * 0.15f, magenta);
            AddPrimitive(root.transform, "Wrist", PrimitiveType.Cylinder, new Vector3(0, 0.1f, 0), new Vector3(0.24f, 0.15f, 0.24f), dark);
            AddGlowRing(root.transform, magenta);

            return root;
        }

        public static GameObject Haste()
        {
            var root = new GameObject("HasteModel");
            var blue = new Color(0.25f, 0.55f, 0.95f);
            var blueLight = new Color(0.6f, 0.8f, 1f);

            AddPrimitive(root.transform, "Face", PrimitiveType.Cylinder, new Vector3(0, 0.3f, 0), new Vector3(0.32f, 0.04f, 0.32f), blueLight);
            var handA = AddPrimitive(root.transform, "HandA", PrimitiveType.Cube, new Vector3(0, 0.32f, 0), new Vector3(0.05f, 0.05f, 0.20f), blue);
            handA.transform.localRotation = Quaternion.Euler(0f, 30f, 0f);
            var handB = AddPrimitive(root.transform, "HandB", PrimitiveType.Cube, new Vector3(0, 0.32f, 0), new Vector3(0.05f, 0.05f, 0.13f), blue);
            handB.transform.localRotation = Quaternion.Euler(0f, 130f, 0f);
            AddGlowRing(root.transform, blue);
            return root;
        }

        public static GameObject AreaBoost()
        {
            var root = new GameObject("AreaBoostModel");
            var green = new Color(0.35f, 0.85f, 0.35f);
            var greenLight = new Color(0.65f, 0.95f, 0.55f);

            AddPrimitive(root.transform, "RingOuter", PrimitiveType.Cylinder, new Vector3(0, 0.04f, 0), new Vector3(0.52f, 0.012f, 0.52f), green);
            AddPrimitive(root.transform, "RingMid", PrimitiveType.Cylinder, new Vector3(0, 0.24f, 0), new Vector3(0.34f, 0.012f, 0.34f), greenLight);
            AddPrimitive(root.transform, "RingInner", PrimitiveType.Cylinder, new Vector3(0, 0.44f, 0), new Vector3(0.18f, 0.012f, 0.18f), green);
            return root;
        }

        public static GameObject Ground(float size = 200f)
        {
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Ground";
            Object.Destroy(plane.GetComponent<Collider>());
            plane.transform.position = Vector3.zero;
            plane.transform.localScale = Vector3.one * (size / 10f); // default Plane is 10x10 units
            plane.GetComponent<MeshRenderer>().sharedMaterial = MaterialCache.Get(new Color(0.28f, 0.42f, 0.24f));
            return plane;
        }

        // Static environment obstacles — unlike the other props these KEEP a
        // collider on the root, since they're meant to physically block
        // movement, not just decorate.
        public static GameObject Rock(float scale = 1f)
        {
            var root = new GameObject("Rock");
            var gray = new Color(0.45f, 0.44f, 0.42f);
            var grayDark = new Color(0.33f, 0.32f, 0.30f);

            AddPrimitive(root.transform, "Base", PrimitiveType.Sphere, new Vector3(0, 0.35f, 0), new Vector3(0.9f, 0.6f, 0.8f), gray);
            AddPrimitive(root.transform, "Bump", PrimitiveType.Sphere, new Vector3(0.25f, 0.55f, 0.1f), new Vector3(0.5f, 0.4f, 0.45f), grayDark);
            AddPrimitive(root.transform, "Bump2", PrimitiveType.Sphere, new Vector3(-0.3f, 0.45f, -0.15f), new Vector3(0.45f, 0.35f, 0.4f), gray);

            root.transform.localScale = Vector3.one * scale;

            var collider = root.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0, 0.35f, 0);
            collider.radius = 0.55f;
            collider.height = 0.9f;

            return root;
        }

        public static GameObject Tree()
        {
            var root = new GameObject("Tree");
            var trunk = new Color(0.35f, 0.24f, 0.16f);
            var leaves = new Color(0.18f, 0.42f, 0.20f);

            AddPrimitive(root.transform, "Trunk", PrimitiveType.Cylinder, new Vector3(0, 0.9f, 0), new Vector3(0.3f, 0.9f, 0.3f), trunk);
            AddPrimitive(root.transform, "Leaves1", PrimitiveType.Sphere, new Vector3(0, 2.1f, 0), Vector3.one * 1.1f, leaves);
            AddPrimitive(root.transform, "Leaves2", PrimitiveType.Sphere, new Vector3(0.4f, 1.8f, 0.3f), Vector3.one * 0.7f, leaves);
            AddPrimitive(root.transform, "Leaves3", PrimitiveType.Sphere, new Vector3(-0.4f, 1.85f, -0.25f), Vector3.one * 0.65f, leaves);

            var collider = root.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0, 0.9f, 0);
            collider.radius = 0.35f;
            collider.height = 1.8f;

            return root;
        }

        // A low bush — decorative only (no collider), for ground-level variety
        // between the taller Rock/Tree obstacles.
        public static GameObject Bush()
        {
            var root = new GameObject("Bush");
            var leaves = new Color(0.22f, 0.48f, 0.20f);
            var leavesDark = new Color(0.16f, 0.36f, 0.15f);

            AddPrimitive(root.transform, "Base", PrimitiveType.Sphere, new Vector3(0, 0.28f, 0), new Vector3(0.7f, 0.5f, 0.65f), leaves);
            AddPrimitive(root.transform, "Bump", PrimitiveType.Sphere, new Vector3(0.25f, 0.4f, 0.1f), new Vector3(0.4f, 0.35f, 0.4f), leavesDark);
            AddPrimitive(root.transform, "Bump2", PrimitiveType.Sphere, new Vector3(-0.22f, 0.35f, -0.15f), new Vector3(0.38f, 0.3f, 0.35f), leavesDark);

            return root;
        }

        // Flat disc used for the attack swipe VFX — opaque (no alpha fade) to
        // avoid needing URP transparency setup for an unlit material.
        public static GameObject SwipeDisc(Color color)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "SwipeDisc";
            Object.Destroy(disc.GetComponent<Collider>());
            disc.transform.localScale = new Vector3(1f, 0.02f, 1f);
            disc.GetComponent<MeshRenderer>().sharedMaterial = MaterialCache.Get(color);
            return disc;
        }

        static GameObject AddPrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = MaterialCache.Get(color);
            return go;
        }

        static GameObject AddCapsule(Transform parent, string name, Vector3 localPos, float radius, float height, Color color)
        {
            return AddPrimitive(parent, name, PrimitiveType.Capsule, localPos, new Vector3(radius * 2f, height / 2f, radius * 2f), color);
        }

        static void AddGlowRing(Transform parent, Color color)
        {
            AddPrimitive(parent, "GlowRing", PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f), new Vector3(0.5f, 0.01f, 0.5f), color);
        }

        // An arm as a shoulder pivot (named `name`, positioned at the shoulder)
        // with the actual capsule as a child hanging below it — rotating the
        // pivot swings the arm naturally instead of spinning it around its
        // own center.
        static GameObject AddArm(Transform parent, string name, Vector3 shoulderLocalPos, float radius, float height, Color color)
        {
            var pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = shoulderLocalPos;

            AddCapsule(pivot.transform, "Visual", new Vector3(0f, -height / 2f, 0f), radius, height, color);
            return pivot;
        }
    }
}
