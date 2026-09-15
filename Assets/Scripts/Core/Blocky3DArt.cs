using UnityEngine;

namespace GorillaSurvivors.Core
{
    public enum HumanVariant { Grunt, Runner, Brute, Thrower, Shieldman, Bomber, Medic }

    // Primitive-assembled 3D models (spheres/capsules/cubes). Since the
    // project ships no art assets, quality comes from silhouette, proportion
    // and palette — the scene's directional light (see GameBootstrap) does
    // the shading, so parts use plain Lit colors rather than hand-painted
    // fake shadows.
    //
    // Limbs are built as a pivot GameObject at the joint with the geometry
    // hanging below it, so rotating the pivot swings the limb naturally.
    // PlayerAttack/QuickSwipeAttack/CharacterAnimator all drive these pivots
    // by name — "ArmL", "ArmR", "LegL", "LegR" and "Head" must stay direct
    // children of the model root.
    public static class Blocky3DArt
    {
        // ---------------------------------------------------------------
        // Player
        // ---------------------------------------------------------------

        public static GameObject Gorilla()
        {
            var root = new GameObject("GorillaModel");

            // Silverback palette: charcoal greys rather than brown, with the
            // saddle bright enough to be the animal's read-at-a-glance
            // marking. Real lighting does the form, so the fur can sit this
            // dark without collapsing into a silhouette — but not truly
            // black, because the fixed camera looks down at 45 degrees and
            // the sky-facing surfaces still need to catch the key light.
            var fur = new Color(0.23f, 0.22f, 0.24f);
            var furMid = new Color(0.30f, 0.29f, 0.32f);
            var furLight = new Color(0.40f, 0.39f, 0.43f);
            var silver = new Color(0.79f, 0.79f, 0.82f);
            var hide = new Color(0.11f, 0.10f, 0.12f);
            var muzzle = new Color(0.16f, 0.15f, 0.17f);

            // Upright, hunched-forward silverback rather than a quadruped
            // crouch: the fixed camera looks down the character's back, and a
            // horizontal body just reads as a shapeless mass from there. A
            // vertical stack of hips / chest / shoulders / head gives the
            // camera four distinct tiers to separate.
            AddPart(root.transform, "Hips", PrimitiveType.Sphere, new Vector3(0f, 0.54f, -0.10f), new Vector3(0.86f, 0.66f, 0.76f), fur);
            AddPart(root.transform, "Torso", PrimitiveType.Sphere, new Vector3(0f, 0.98f, 0.02f), new Vector3(1.10f, 1.00f, 0.90f), furMid);
            AddPart(root.transform, "Saddle", PrimitiveType.Sphere, new Vector3(0f, 1.20f, -0.22f), new Vector3(0.80f, 0.46f, 0.54f), silver);
            AddPart(root.transform, "ChestL", PrimitiveType.Sphere, new Vector3(-0.24f, 1.02f, 0.36f), new Vector3(0.46f, 0.42f, 0.28f), hide);
            AddPart(root.transform, "ChestR", PrimitiveType.Sphere, new Vector3(0.24f, 1.02f, 0.36f), new Vector3(0.46f, 0.42f, 0.28f), hide);
            AddPart(root.transform, "ShoulderL", PrimitiveType.Sphere, new Vector3(-0.58f, 1.34f, 0f), Vector3.one * 0.50f, furLight);
            AddPart(root.transform, "ShoulderR", PrimitiveType.Sphere, new Vector3(0.58f, 1.34f, 0f), Vector3.one * 0.50f, furLight);
            AddPart(root.transform, "Trap", PrimitiveType.Sphere, new Vector3(0f, 1.42f, -0.08f), new Vector3(0.80f, 0.34f, 0.46f), furMid);
            AddPart(root.transform, "Neck", PrimitiveType.Sphere, new Vector3(0f, 1.50f, 0.10f), new Vector3(0.40f, 0.30f, 0.34f), fur);

            // Facial features parent to Head so RoarAbility's head pulse
            // scales the whole face, not a bare skull sphere.
            var head = AddPart(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.74f, 0.14f), new Vector3(0.62f, 0.60f, 0.58f), furMid);
            AddPart(head.transform, "Crest", PrimitiveType.Sphere, new Vector3(0f, 0.34f, -0.08f), new Vector3(0.60f, 0.52f, 0.72f), furLight);
            AddPart(head.transform, "Brow", PrimitiveType.Sphere, new Vector3(0f, 0.16f, 0.40f), new Vector3(0.90f, 0.26f, 0.42f), hide);
            AddPart(head.transform, "Muzzle", PrimitiveType.Sphere, new Vector3(0f, -0.20f, 0.44f), new Vector3(0.62f, 0.46f, 0.52f), muzzle);
            AddPart(head.transform, "Jaw", PrimitiveType.Sphere, new Vector3(0f, -0.40f, 0.30f), new Vector3(0.56f, 0.30f, 0.44f), muzzle);
            AddPart(head.transform, "NostrilL", PrimitiveType.Sphere, new Vector3(-0.11f, -0.16f, 0.66f), Vector3.one * 0.09f, hide);
            AddPart(head.transform, "NostrilR", PrimitiveType.Sphere, new Vector3(0.11f, -0.16f, 0.66f), Vector3.one * 0.09f, hide);
            AddPart(head.transform, "EyeL", PrimitiveType.Sphere, new Vector3(-0.20f, 0.05f, 0.46f), Vector3.one * 0.13f, new Color(0.93f, 0.88f, 0.78f));
            AddPart(head.transform, "EyeR", PrimitiveType.Sphere, new Vector3(0.20f, 0.05f, 0.46f), Vector3.one * 0.13f, new Color(0.93f, 0.88f, 0.78f));
            AddPart(head.transform, "PupilL", PrimitiveType.Sphere, new Vector3(-0.20f, 0.04f, 0.52f), Vector3.one * 0.075f, Color.black);
            AddPart(head.transform, "PupilR", PrimitiveType.Sphere, new Vector3(0.20f, 0.04f, 0.52f), Vector3.one * 0.075f, Color.black);
            AddPart(head.transform, "EarL", PrimitiveType.Sphere, new Vector3(-0.50f, 0.10f, -0.04f), new Vector3(0.18f, 0.24f, 0.12f), muzzle);
            AddPart(head.transform, "EarR", PrimitiveType.Sphere, new Vector3(0.50f, 0.10f, -0.04f), new Vector3(0.18f, 0.24f, 0.12f), muzzle);

            // Long, heavy ape arms — they hang nearly to the ground, which is
            // most of what makes the silhouette read as "gorilla".
            AddLimb(root.transform, "ArmL", new Vector3(-0.64f, 1.34f, 0.02f), 0.19f, 0.50f, 0.17f, 0.46f, 0.22f, furMid, furLight, hide);
            AddLimb(root.transform, "ArmR", new Vector3(0.64f, 1.34f, 0.02f), 0.19f, 0.50f, 0.17f, 0.46f, 0.22f, furMid, furLight, hide);
            // Short, stocky legs tucked under the bulk.
            AddLimb(root.transform, "LegL", new Vector3(-0.30f, 0.56f, -0.04f), 0.19f, 0.28f, 0.17f, 0.22f, 0.19f, fur, fur, hide);
            AddLimb(root.transform, "LegR", new Vector3(0.30f, 0.56f, -0.04f), 0.19f, 0.28f, 0.17f, 0.22f, 0.19f, fur, fur, hide);

            return root;
        }

        // ---------------------------------------------------------------
        // Enemies
        // ---------------------------------------------------------------

        public static GameObject Human(HumanVariant variant = HumanVariant.Grunt, EnemyModifierRoll modifiers = default)
        {
            var root = new GameObject("HumanModel");

            var skin = new Color(0.76f, 0.58f, 0.46f);
            var pants = new Color(0.24f, 0.25f, 0.31f);
            var shoe = new Color(0.12f, 0.11f, 0.11f);
            var belt = new Color(0.13f, 0.10f, 0.09f);

            Color shirt = new Color(0.72f, 0.20f, 0.18f);
            Color hair = new Color(0.18f, 0.13f, 0.10f);
            float scale = 1f;
            float build = 1f;      // torso/limb thickness
            bool holdsWeapon = false;

            switch (variant)
            {
                case HumanVariant.Runner:
                    shirt = new Color(0.26f, 0.66f, 0.34f);
                    hair = new Color(0.42f, 0.28f, 0.12f);
                    scale = 0.88f;
                    build = 0.85f;
                    break;
                case HumanVariant.Brute:
                    shirt = new Color(0.40f, 0.17f, 0.50f);
                    scale = 1.5f;
                    build = 1.28f;
                    break;
                case HumanVariant.Thrower:
                    shirt = new Color(0.82f, 0.52f, 0.16f);
                    scale = 1f;
                    holdsWeapon = true;
                    break;
                case HumanVariant.Shieldman:
                    shirt = new Color(0.30f, 0.36f, 0.62f);
                    scale = 1.08f;
                    build = 1.1f;
                    break;
                case HumanVariant.Bomber:
                    shirt = new Color(0.85f, 0.78f, 0.22f);
                    hair = new Color(0.30f, 0.22f, 0.16f);
                    scale = 0.95f;
                    break;
                case HumanVariant.Medic:
                    shirt = new Color(0.92f, 0.92f, 0.94f);
                    hair = new Color(0.55f, 0.45f, 0.30f);
                    scale = 1f;
                    build = 0.92f;
                    break;
            }

            var weaponColor = new Color(0.33f, 0.24f, 0.15f);
            if (modifiers.Weapon != ModifierTier.None)
            {
                holdsWeapon = true;
                weaponColor = EnemyModifierRoll.TierColor(modifiers.Weapon);
            }

            AddLimb(root.transform, "LegL", new Vector3(-0.15f * build, 0.66f, 0f), 0.10f * build, 0.30f, 0.09f * build, 0.28f, 0.11f, pants, pants, shoe);
            AddLimb(root.transform, "LegR", new Vector3(0.15f * build, 0.66f, 0f), 0.10f * build, 0.30f, 0.09f * build, 0.28f, 0.11f, pants, pants, shoe);

            AddPart(root.transform, "Hips", PrimitiveType.Sphere, new Vector3(0f, 0.72f, 0f), new Vector3(0.38f * build, 0.26f, 0.28f * build), pants);
            AddPart(root.transform, "Torso", PrimitiveType.Capsule, new Vector3(0f, 1.00f, 0f), new Vector3(0.44f * build, 0.30f, 0.30f * build), shirt);
            AddPart(root.transform, "Belt", PrimitiveType.Cube, new Vector3(0f, 0.80f, 0f), new Vector3(0.46f * build, 0.07f, 0.32f * build), belt);
            AddPart(root.transform, "Neck", PrimitiveType.Capsule, new Vector3(0f, 1.26f, 0f), new Vector3(0.13f, 0.06f, 0.13f), skin);

            var head = AddPart(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.42f, 0f), new Vector3(0.30f, 0.34f, 0.30f), skin);
            AddPart(head.transform, "Hair", PrimitiveType.Sphere, new Vector3(0f, 0.22f, -0.06f), new Vector3(1.08f, 0.72f, 1.10f), hair);
            AddPart(head.transform, "Nose", PrimitiveType.Sphere, new Vector3(0f, -0.06f, 0.48f), new Vector3(0.18f, 0.16f, 0.16f), skin);
            AddPart(head.transform, "EyeL", PrimitiveType.Sphere, new Vector3(-0.28f, 0.06f, 0.40f), new Vector3(0.16f, 0.14f, 0.10f), Color.black);
            AddPart(head.transform, "EyeR", PrimitiveType.Sphere, new Vector3(0.28f, 0.06f, 0.40f), new Vector3(0.16f, 0.14f, 0.10f), Color.black);

            AddLimb(root.transform, "ArmL", new Vector3(-0.28f * build, 1.18f, 0f), 0.085f * build, 0.26f, 0.075f * build, 0.24f, 0.09f, shirt, skin, skin);
            AddLimb(root.transform, "ArmR", new Vector3(0.28f * build, 1.18f, 0f), 0.085f * build, 0.26f, 0.075f * build, 0.24f, 0.09f, shirt, skin, skin);

            if (holdsWeapon)
            {
                var weapon = AddPart(root.transform, "Weapon", PrimitiveType.Capsule, new Vector3(0.38f, 0.82f, 0.18f), new Vector3(0.07f, 0.26f, 0.07f), weaponColor);
                weapon.transform.localRotation = Quaternion.Euler(62f, 0f, 18f);
                AddPart(weapon.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.05f, 0f), new Vector3(1.9f, 0.55f, 1.9f), weaponColor);
            }

            BuildVariantProps(root, variant, scale, build);
            BuildModifierProps(root, modifiers);

            root.transform.localScale = Vector3.one * scale;
            return root;
        }

        // Per-type silhouette cues so the player can read a threat at a
        // glance without relying on shirt color alone.
        static void BuildVariantProps(GameObject root, HumanVariant variant, float scale, float build)
        {
            switch (variant)
            {
                case HumanVariant.Shieldman:
                {
                    // Big slab held out front — the visual promise that
                    // frontal hits get soaked.
                    var shield = AddPart(root.transform, "Shield", PrimitiveType.Cube, new Vector3(0f, 1.02f, 0.42f), new Vector3(0.78f, 0.86f, 0.09f), new Color(0.52f, 0.54f, 0.58f));
                    AddPart(shield.transform, "Boss", PrimitiveType.Sphere, new Vector3(0f, 0f, -1.1f), new Vector3(0.36f, 0.32f, 1.6f), new Color(0.70f, 0.72f, 0.76f));
                    AddPart(shield.transform, "RimTop", PrimitiveType.Cube, new Vector3(0f, 0.46f, 0f), new Vector3(1.06f, 0.10f, 1.5f), new Color(0.38f, 0.39f, 0.42f));
                    break;
                }
                case HumanVariant.Bomber:
                {
                    // Live charge strapped to the chest, fuse and all.
                    var bomb = AddPart(root.transform, "Bomb", PrimitiveType.Sphere, new Vector3(0f, 1.02f, 0.30f), Vector3.one * 0.34f, new Color(0.16f, 0.16f, 0.18f));
                    AddPart(bomb.transform, "Fuse", PrimitiveType.Capsule, new Vector3(0f, 0.62f, 0f), new Vector3(0.14f, 0.28f, 0.14f), new Color(0.62f, 0.52f, 0.34f));
                    AddGlowPart(bomb.transform, "Spark", PrimitiveType.Sphere, new Vector3(0f, 1.05f, 0f), Vector3.one * 0.26f, new Color(1f, 0.62f, 0.15f));
                    break;
                }
                case HumanVariant.Medic:
                {
                    // Red cross on the chest and a satchel on the hip.
                    AddPart(root.transform, "CrossV", PrimitiveType.Cube, new Vector3(0f, 1.05f, 0.17f), new Vector3(0.09f, 0.26f, 0.04f), new Color(0.85f, 0.18f, 0.18f));
                    AddPart(root.transform, "CrossH", PrimitiveType.Cube, new Vector3(0f, 1.05f, 0.17f), new Vector3(0.26f, 0.09f, 0.04f), new Color(0.85f, 0.18f, 0.18f));
                    AddPart(root.transform, "Satchel", PrimitiveType.Cube, new Vector3(0.26f, 0.80f, -0.06f), new Vector3(0.20f, 0.18f, 0.14f), new Color(0.55f, 0.48f, 0.38f));
                    break;
                }
                case HumanVariant.Brute:
                {
                    AddPart(root.transform, "BrowRidge", PrimitiveType.Cube, new Vector3(0f, 1.50f, 0.22f), new Vector3(0.34f, 0.07f, 0.10f), new Color(0.20f, 0.15f, 0.12f));
                    break;
                }
                case HumanVariant.Runner:
                {
                    AddPart(root.transform, "Headband", PrimitiveType.Cube, new Vector3(0f, 1.52f, 0f), new Vector3(0.34f, 0.07f, 0.34f), new Color(0.90f, 0.30f, 0.30f));
                    break;
                }
            }
        }

        static void AttachShoe(Transform leg, string name, Color color)
        {
            if (leg == null) return;
            var foot = leg.Find("Lower/End");
            if (foot == null) return;

            // Parent to the knee joint ("Lower") beside the foot sphere rather
            // than to the sphere itself, so the shoe isn't inheriting the
            // foot's non-uniform scale; it sits just forward like a toe box.
            var shoe = AddGlowPart(foot.parent, name, PrimitiveType.Sphere, foot.localPosition + new Vector3(0f, -0.02f, 0.06f), new Vector3(0.19f, 0.12f, 0.26f), color);
            shoe.transform.localRotation = Quaternion.identity;
        }

        static void BuildModifierProps(GameObject root, EnemyModifierRoll modifiers)
        {
            if (modifiers.Armor != ModifierTier.None)
            {
                var tint = EnemyModifierRoll.TierColor(modifiers.Armor);
                var plate = AddMetalPart(root.transform, "Armor", PrimitiveType.Capsule, new Vector3(0f, 1.00f, 0.02f), new Vector3(0.50f, 0.28f, 0.36f), tint);
                AddMetalPart(plate.transform, "PauldronL", PrimitiveType.Sphere, new Vector3(-0.62f, 0.55f, 0f), new Vector3(0.52f, 0.72f, 0.72f), tint);
                AddMetalPart(plate.transform, "PauldronR", PrimitiveType.Sphere, new Vector3(0.62f, 0.55f, 0f), new Vector3(0.52f, 0.72f, 0.72f), tint);
            }

            if (modifiers.HasShoes)
            {
                // Parented to each leg's foot joint rather than the model root,
                // so the shoes travel with the stride instead of sitting still
                // on the ground under a walking enemy.
                var glow = new Color(0.95f, 0.85f, 0.15f);
                AttachShoe(root.transform.Find("LegL"), "ShoeGlowL", glow);
                AttachShoe(root.transform.Find("LegR"), "ShoeGlowR", glow);
            }

            if (modifiers.HasCrown)
            {
                var gold = new Color(1f, 0.82f, 0.22f);
                AddMetalPart(root.transform, "CrownBand", PrimitiveType.Cylinder, new Vector3(0f, 1.60f, 0f), new Vector3(0.27f, 0.045f, 0.27f), gold);

                const int spikeCount = 6;
                const float ringRadius = 0.20f;
                for (int i = 0; i < spikeCount; i++)
                {
                    float angle = i * Mathf.PI * 2f / spikeCount;
                    float x = Mathf.Cos(angle) * ringRadius;
                    float z = Mathf.Sin(angle) * ringRadius;
                    bool tall = i % 2 == 0;
                    float h = tall ? 0.20f : 0.13f;
                    // Rotating each spike 45 degrees on Y turns the square
                    // cross-section into a diamond, which reads as a point
                    // rather than a little post.
                    var spike = AddMetalPart(root.transform, "CrownSpike" + i, PrimitiveType.Cube, new Vector3(x, 1.63f + h / 2f, z), new Vector3(0.05f, h, 0.05f), gold);
                    spike.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                    AddMetalPart(root.transform, "CrownTip" + i, PrimitiveType.Sphere, new Vector3(x, 1.63f + h, z), Vector3.one * 0.05f, gold);
                }
            }
        }

        // ---------------------------------------------------------------
        // Pickups
        // ---------------------------------------------------------------

        public static GameObject Gem()
        {
            var root = new GameObject("GemModel");
            var gold = new Color(0.98f, 0.82f, 0.18f);
            var top = AddGlowPart(root.transform, "Crown", PrimitiveType.Cube, new Vector3(0f, 0.34f, 0f), new Vector3(0.22f, 0.22f, 0.22f), gold);
            top.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            var bot = AddGlowPart(root.transform, "Point", PrimitiveType.Cube, new Vector3(0f, 0.22f, 0f), new Vector3(0.15f, 0.15f, 0.15f), new Color(1f, 0.92f, 0.55f));
            bot.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            return root;
        }

        public static GameObject Banana()
        {
            var root = new GameObject("BananaModel");
            var yellow = new Color(0.95f, 0.82f, 0.15f);
            var yellowDark = new Color(0.82f, 0.66f, 0.10f);
            var tip = new Color(0.40f, 0.30f, 0.10f);

            AddOneBanana(root.transform, new Vector3(-0.07f, 0f, 0.04f), 18f, yellow, tip);
            AddOneBanana(root.transform, new Vector3(0.09f, 0.04f, -0.05f), -24f, yellowDark, tip);
            AddGlowRing(root.transform, yellow);
            return root;
        }

        static void AddOneBanana(Transform parent, Vector3 offset, float tiltZ, Color color, Color tip)
        {
            var group = new GameObject("Banana");
            group.transform.SetParent(parent, false);
            group.transform.localPosition = offset;

            // Three short segments arced end to end read as a curved banana,
            // where one straight capsule reads as a sausage.
            for (int i = 0; i < 3; i++)
            {
                float t = i / 2f;
                float bend = Mathf.Sin(t * Mathf.PI) * 0.10f;
                var seg = AddPart(group.transform, "Seg" + i, PrimitiveType.Capsule, new Vector3(-0.14f + t * 0.28f, 0.34f + bend, 0f), new Vector3(0.115f, 0.10f, 0.115f), color);
                seg.transform.localRotation = Quaternion.Euler(0f, 0f, 60f + tiltZ - t * 40f);
            }
            AddPart(group.transform, "TipA", PrimitiveType.Sphere, new Vector3(-0.17f, 0.31f, 0f), Vector3.one * 0.06f, tip);
            AddPart(group.transform, "TipB", PrimitiveType.Sphere, new Vector3(0.17f, 0.31f, 0f), Vector3.one * 0.06f, tip);
        }

        public static GameObject Adrenaline()
        {
            var root = new GameObject("AdrenalineModel");
            var cyan = new Color(0.25f, 0.85f, 0.95f);
            var cyanLight = new Color(0.62f, 0.96f, 1f);

            var barrel = AddPart(root.transform, "Barrel", PrimitiveType.Capsule, new Vector3(0f, 0.36f, 0f), new Vector3(0.16f, 0.20f, 0.16f), cyanLight);
            barrel.transform.localRotation = Quaternion.Euler(0f, 0f, 22f);
            AddPart(root.transform, "Plunger", PrimitiveType.Cylinder, new Vector3(-0.10f, 0.60f, 0f), new Vector3(0.18f, 0.03f, 0.18f), cyan);
            AddPart(root.transform, "Needle", PrimitiveType.Capsule, new Vector3(0.14f, 0.12f, 0f), new Vector3(0.035f, 0.10f, 0.035f), new Color(0.85f, 0.88f, 0.92f));
            AddGlowRing(root.transform, cyan);
            return root;
        }

        public static GameObject Rampage()
        {
            var root = new GameObject("RampageModel");
            var magenta = new Color(0.85f, 0.20f, 0.75f);
            var dark = new Color(0.52f, 0.09f, 0.45f);

            AddPart(root.transform, "Fist", PrimitiveType.Sphere, new Vector3(0f, 0.40f, 0f), new Vector3(0.42f, 0.38f, 0.36f), magenta);
            for (int i = 0; i < 4; i++)
            {
                float x = -0.16f + i * 0.105f;
                AddPart(root.transform, "Knuckle" + i, PrimitiveType.Sphere, new Vector3(x, 0.57f, 0.07f), Vector3.one * 0.13f, magenta);
            }
            AddPart(root.transform, "Thumb", PrimitiveType.Sphere, new Vector3(0.20f, 0.44f, 0.12f), new Vector3(0.14f, 0.18f, 0.14f), magenta);
            AddPart(root.transform, "Wrist", PrimitiveType.Cylinder, new Vector3(0f, 0.14f, 0f), new Vector3(0.26f, 0.14f, 0.26f), dark);
            AddGlowRing(root.transform, magenta);
            return root;
        }

        public static GameObject Haste()
        {
            var root = new GameObject("HasteModel");
            var blue = new Color(0.25f, 0.55f, 0.95f);
            var blueLight = new Color(0.66f, 0.84f, 1f);

            AddPart(root.transform, "Face", PrimitiveType.Cylinder, new Vector3(0f, 0.32f, 0f), new Vector3(0.34f, 0.045f, 0.34f), blueLight);
            AddPart(root.transform, "Rim", PrimitiveType.Cylinder, new Vector3(0f, 0.30f, 0f), new Vector3(0.40f, 0.04f, 0.40f), blue);
            var handA = AddGlowPart(root.transform, "HandA", PrimitiveType.Cube, new Vector3(0f, 0.36f, 0f), new Vector3(0.045f, 0.03f, 0.22f), blue);
            handA.transform.localRotation = Quaternion.Euler(0f, 30f, 0f);
            var handB = AddGlowPart(root.transform, "HandB", PrimitiveType.Cube, new Vector3(0f, 0.36f, 0f), new Vector3(0.045f, 0.03f, 0.14f), blue);
            handB.transform.localRotation = Quaternion.Euler(0f, 130f, 0f);
            AddGlowRing(root.transform, blue);
            return root;
        }

        public static GameObject AreaBoost()
        {
            var root = new GameObject("AreaBoostModel");
            var green = new Color(0.35f, 0.85f, 0.35f);
            var greenLight = new Color(0.68f, 0.96f, 0.58f);

            AddGlowPart(root.transform, "RingOuter", PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.54f, 0.012f, 0.54f), green);
            AddGlowPart(root.transform, "RingMid", PrimitiveType.Cylinder, new Vector3(0f, 0.26f, 0f), new Vector3(0.36f, 0.012f, 0.36f), greenLight);
            AddGlowPart(root.transform, "RingInner", PrimitiveType.Cylinder, new Vector3(0f, 0.46f, 0f), new Vector3(0.19f, 0.012f, 0.19f), green);
            AddPart(root.transform, "Core", PrimitiveType.Sphere, new Vector3(0f, 0.26f, 0f), Vector3.one * 0.12f, greenLight);
            return root;
        }

        // ---------------------------------------------------------------
        // Environment
        // ---------------------------------------------------------------

        public static GameObject Ground(float size = 220f)
        {
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Ground";
            Object.Destroy(plane.GetComponent<Collider>());
            plane.transform.position = Vector3.zero;
            plane.transform.localScale = Vector3.one * (size / 10f); // default Plane is 10x10 units

            // One tile every ~4 world units: close enough to give the surface
            // texture at player scale, far enough not to shimmer at distance.
            float tiles = size / 4f;
            plane.GetComponent<MeshRenderer>().sharedMaterial =
                MaterialCache.GetTextured(ProceduralTextures.Grass(), Color.white, new Vector2(tiles, tiles));
            return plane;
        }

        public static GameObject Rock(float scale = 1f)
        {
            var root = new GameObject("Rock");
            var gray = new Color(0.46f, 0.45f, 0.43f);
            var grayDark = new Color(0.33f, 0.32f, 0.31f);
            var grayLight = new Color(0.58f, 0.57f, 0.55f);

            // Angular chunks (rotated cubes) mixed with spheres so rocks read
            // as stone rather than as a pile of eggs.
            var core = AddPart(root.transform, "Core", PrimitiveType.Cube, new Vector3(0f, 0.33f, 0f), new Vector3(0.80f, 0.56f, 0.72f), gray);
            core.transform.localRotation = Quaternion.Euler(8f, 24f, 6f);
            var chunk = AddPart(root.transform, "Chunk", PrimitiveType.Cube, new Vector3(0.26f, 0.48f, 0.08f), new Vector3(0.44f, 0.40f, 0.42f), grayLight);
            chunk.transform.localRotation = Quaternion.Euler(-14f, 40f, 18f);
            var chunk2 = AddPart(root.transform, "Chunk2", PrimitiveType.Cube, new Vector3(-0.28f, 0.36f, -0.12f), new Vector3(0.42f, 0.34f, 0.38f), grayDark);
            chunk2.transform.localRotation = Quaternion.Euler(12f, -30f, -10f);
            AddPart(root.transform, "Pebble", PrimitiveType.Sphere, new Vector3(0.34f, 0.10f, -0.26f), new Vector3(0.24f, 0.18f, 0.22f), grayDark);

            root.transform.localScale = Vector3.one * scale;

            var collider = root.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.35f, 0f);
            collider.radius = 0.52f;
            collider.height = 0.9f;
            return root;
        }

        // Tree geometry lives entirely above the root's origin so the whole
        // thing can be tipped over by rotating the root — see FellableTree.
        public static GameObject Tree(int species = -1)
        {
            var root = new GameObject("Tree");
            if (species < 0) species = Random.Range(0, 3);

            var bark = new Color(0.34f, 0.24f, 0.17f);
            var barkDark = new Color(0.25f, 0.17f, 0.12f);

            switch (species)
            {
                case 0: // Broad canopy
                {
                    var leaves = new Color(0.20f, 0.42f, 0.20f);
                    var leavesLight = new Color(0.27f, 0.51f, 0.24f);
                    AddPart(root.transform, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 0.95f, 0f), new Vector3(0.30f, 0.95f, 0.30f), bark);
                    AddPart(root.transform, "Flare", PrimitiveType.Cylinder, new Vector3(0f, 0.12f, 0f), new Vector3(0.44f, 0.14f, 0.44f), barkDark);
                    AddPart(root.transform, "Canopy", PrimitiveType.Sphere, new Vector3(0f, 2.15f, 0f), new Vector3(1.25f, 1.05f, 1.25f), leaves);
                    AddPart(root.transform, "CanopyB", PrimitiveType.Sphere, new Vector3(0.45f, 1.85f, 0.30f), Vector3.one * 0.78f, leavesLight);
                    AddPart(root.transform, "CanopyC", PrimitiveType.Sphere, new Vector3(-0.42f, 1.92f, -0.26f), Vector3.one * 0.70f, leaves);
                    AddPart(root.transform, "CanopyD", PrimitiveType.Sphere, new Vector3(0.05f, 2.62f, -0.12f), Vector3.one * 0.66f, leavesLight);
                    break;
                }
                case 1: // Conifer
                {
                    var needle = new Color(0.15f, 0.34f, 0.22f);
                    var needleLight = new Color(0.20f, 0.42f, 0.26f);
                    AddPart(root.transform, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 0.70f, 0f), new Vector3(0.24f, 0.70f, 0.24f), barkDark);
                    for (int i = 0; i < 4; i++)
                    {
                        float t = i / 3f;
                        float y = 1.20f + i * 0.62f;
                        float r = Mathf.Lerp(1.15f, 0.34f, t);
                        AddPart(root.transform, "Tier" + i, PrimitiveType.Cylinder, new Vector3(0f, y, 0f), new Vector3(r, 0.34f, r), i % 2 == 0 ? needle : needleLight);
                    }
                    AddPart(root.transform, "Top", PrimitiveType.Capsule, new Vector3(0f, 3.55f, 0f), new Vector3(0.22f, 0.26f, 0.22f), needle);
                    break;
                }
                default: // Tall palm-ish with a leaning trunk
                {
                    var frond = new Color(0.24f, 0.47f, 0.22f);
                    var frondDark = new Color(0.18f, 0.38f, 0.18f);
                    for (int i = 0; i < 4; i++)
                    {
                        float t = i / 3f;
                        AddPart(root.transform, "Trunk" + i, PrimitiveType.Cylinder, new Vector3(t * t * 0.28f, 0.42f + i * 0.72f, 0f), new Vector3(0.24f - t * 0.06f, 0.38f, 0.24f - t * 0.06f), i % 2 == 0 ? bark : barkDark);
                    }
                    for (int i = 0; i < 6; i++)
                    {
                        float angle = i * 60f;
                        var frondGO = AddPart(root.transform, "Frond" + i, PrimitiveType.Capsule, Vector3.zero, new Vector3(0.20f, 0.52f, 0.20f), i % 2 == 0 ? frond : frondDark);
                        frondGO.transform.localPosition = new Vector3(0.28f, 3.05f, 0f) + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.52f, -0.10f, 0f);
                        frondGO.transform.localRotation = Quaternion.Euler(0f, angle, 74f);
                    }
                    AddPart(root.transform, "Crown", PrimitiveType.Sphere, new Vector3(0.28f, 3.08f, 0f), Vector3.one * 0.32f, frondDark);
                    break;
                }
            }

            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.radius = 0.33f;
            col.height = 1.8f;
            return root;
        }

        public static GameObject Bush()
        {
            var root = new GameObject("Bush");
            var leaves = new Color(0.23f, 0.47f, 0.21f);
            var leavesDark = new Color(0.17f, 0.36f, 0.16f);
            var leavesLight = new Color(0.31f, 0.55f, 0.26f);

            AddPart(root.transform, "Base", PrimitiveType.Sphere, new Vector3(0f, 0.26f, 0f), new Vector3(0.72f, 0.48f, 0.66f), leaves);
            AddPart(root.transform, "Bump", PrimitiveType.Sphere, new Vector3(0.24f, 0.38f, 0.10f), new Vector3(0.42f, 0.36f, 0.40f), leavesLight);
            AddPart(root.transform, "Bump2", PrimitiveType.Sphere, new Vector3(-0.22f, 0.34f, -0.14f), new Vector3(0.40f, 0.32f, 0.36f), leavesDark);
            AddPart(root.transform, "Bump3", PrimitiveType.Sphere, new Vector3(0.02f, 0.50f, -0.04f), new Vector3(0.34f, 0.28f, 0.32f), leaves);
            return root;
        }

        // Small ground dressing — no colliders, purely to break up the plane.
        public static GameObject GrassTuft()
        {
            var root = new GameObject("GrassTuft");
            var a = new Color(0.32f, 0.50f, 0.22f);
            var b = new Color(0.26f, 0.43f, 0.19f);

            int blades = Random.Range(4, 7);
            for (int i = 0; i < blades; i++)
            {
                float angle = Random.Range(0f, 360f);
                float lean = Random.Range(8f, 26f);
                float h = Random.Range(0.16f, 0.32f);
                var blade = AddPart(root.transform, "Blade" + i, PrimitiveType.Capsule, Vector3.zero, new Vector3(0.05f, h, 0.05f), i % 2 == 0 ? a : b);
                blade.transform.localPosition = Quaternion.Euler(0f, angle, 0f) * new Vector3(Random.Range(0.02f, 0.14f), h * 0.8f, 0f);
                blade.transform.localRotation = Quaternion.Euler(0f, angle, lean);
            }
            DisableShadowCasting(root);
            return root;
        }

        // Ground dressing is scattered in the hundreds and is too small for
        // its shadows to read — keeping them out of the shadow pass is most
        // of the cost of having that much of it.
        static void DisableShadowCasting(GameObject root)
        {
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>())
            {
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        public static GameObject Flower()
        {
            var root = new GameObject("Flower");
            Color[] petals = { new Color(0.92f, 0.86f, 0.30f), new Color(0.88f, 0.42f, 0.62f), new Color(0.72f, 0.60f, 0.92f), new Color(0.95f, 0.95f, 0.92f) };
            var petal = petals[Random.Range(0, petals.Length)];

            AddPart(root.transform, "Stem", PrimitiveType.Capsule, new Vector3(0f, 0.14f, 0f), new Vector3(0.035f, 0.14f, 0.035f), new Color(0.28f, 0.45f, 0.20f));
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 72f;
                var p = AddPart(root.transform, "Petal" + i, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.09f, 0.03f, 0.06f), petal);
                p.transform.localPosition = new Vector3(0f, 0.30f, 0f) + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.07f, 0f, 0f);
                p.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
            }
            AddPart(root.transform, "Center", PrimitiveType.Sphere, new Vector3(0f, 0.31f, 0f), Vector3.one * 0.055f, new Color(0.95f, 0.75f, 0.20f));
            DisableShadowCasting(root);
            return root;
        }

        public static GameObject Stump()
        {
            var root = new GameObject("Stump");
            var bark = new Color(0.30f, 0.21f, 0.15f);
            var inner = new Color(0.62f, 0.48f, 0.32f);

            AddPart(root.transform, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 0.22f, 0f), new Vector3(0.52f, 0.22f, 0.52f), bark);
            AddPart(root.transform, "Rings", PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f), new Vector3(0.46f, 0.02f, 0.46f), inner);
            AddPart(root.transform, "RootA", PrimitiveType.Sphere, new Vector3(0.34f, 0.08f, 0.14f), new Vector3(0.28f, 0.14f, 0.22f), bark);
            AddPart(root.transform, "RootB", PrimitiveType.Sphere, new Vector3(-0.30f, 0.08f, -0.18f), new Vector3(0.26f, 0.13f, 0.20f), bark);
            return root;
        }

        public static GameObject Mushroom()
        {
            var root = new GameObject("Mushroom");
            var cap = Random.value < 0.5f ? new Color(0.78f, 0.24f, 0.20f) : new Color(0.70f, 0.55f, 0.38f);
            AddPart(root.transform, "Stalk", PrimitiveType.Capsule, new Vector3(0f, 0.10f, 0f), new Vector3(0.07f, 0.09f, 0.07f), new Color(0.90f, 0.87f, 0.78f));
            AddPart(root.transform, "Cap", PrimitiveType.Sphere, new Vector3(0f, 0.20f, 0f), new Vector3(0.22f, 0.14f, 0.22f), cap);
            AddPart(root.transform, "Spot", PrimitiveType.Sphere, new Vector3(0.05f, 0.25f, 0.04f), Vector3.one * 0.05f, new Color(0.95f, 0.93f, 0.88f));
            DisableShadowCasting(root);
            return root;
        }

        // ---------------------------------------------------------------
        // VFX
        // ---------------------------------------------------------------

        // Flat disc for swipe/shockwave VFX — unlit so it stays bright
        // regardless of where the light is.
        public static GameObject SwipeDisc(Color color)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "SwipeDisc";
            Object.Destroy(disc.GetComponent<Collider>());
            disc.transform.localScale = new Vector3(1f, 0.02f, 1f);
            var mr = disc.GetComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialCache.GetUnlit(color);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return disc;
        }

        // ---------------------------------------------------------------
        // Builders
        // ---------------------------------------------------------------

        static GameObject AddPart(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Color color)
        {
            var go = CreateBarePrimitive(type, name, parent);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = MaterialCache.Get(color);
            return go;
        }

        static GameObject AddMetalPart(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Color color)
        {
            var go = CreateBarePrimitive(type, name, parent);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = MaterialCache.GetMetallic(color);
            return go;
        }

        static GameObject AddGlowPart(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Color color)
        {
            var go = CreateBarePrimitive(type, name, parent);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = MaterialCache.GetGlowing(color);
            return go;
        }

        static GameObject CreateBarePrimitive(PrimitiveType type, string name, Transform parent)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            // Model parts are visual only — the owning GameObject carries the
            // single collider that physics and attack hit-scans care about.
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            return go;
        }

        static void AddGlowRing(Transform parent, Color color)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "GlowRing";
            Object.Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(parent, false);
            ring.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            ring.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);
            var mr = ring.GetComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialCache.GetUnlit(color);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // A limb as a shoulder/hip pivot with upper segment, elbow/knee pivot,
        // lower segment and an end cap (hand/foot). Rotating the top pivot
        // swings the whole limb; rotating the "Lower" child bends the joint.
        static GameObject AddLimb(Transform parent, string name, Vector3 pivotLocalPos,
            float upperRadius, float upperLength,
            float lowerRadius, float lowerLength,
            float endRadius,
            Color upperColor, Color lowerColor, Color endColor)
        {
            var pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = pivotLocalPos;

            AddPart(pivot.transform, "Upper", PrimitiveType.Capsule, new Vector3(0f, -upperLength / 2f, 0f), new Vector3(upperRadius * 2f, upperLength / 2f, upperRadius * 2f), upperColor);

            var lower = new GameObject("Lower");
            lower.transform.SetParent(pivot.transform, false);
            lower.transform.localPosition = new Vector3(0f, -upperLength, 0f);

            AddPart(lower.transform, "Visual", PrimitiveType.Capsule, new Vector3(0f, -lowerLength / 2f, 0f), new Vector3(lowerRadius * 2f, lowerLength / 2f, lowerRadius * 2f), lowerColor);
            AddPart(lower.transform, "End", PrimitiveType.Sphere, new Vector3(0f, -lowerLength, 0f), Vector3.one * endRadius * 2f, endColor);

            return pivot;
        }
    }
}
