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

            // Ape build, not a hunched man. The defining difference is where
            // the mass sits: a gorilla's shoulder girdle is pushed FORWARD
            // and carries most of the bulk, the spine slopes down and back to
            // small hips, and the head hangs low in FRONT of the shoulders
            // rather than perched on top. Shoulders level with the ribcage
            // with arms at the sides is exactly the human read.
            //
            // So the body is built as a wedge: narrow rump at the back, deep
            // barrel chest forward, and a shoulder hump that is the highest
            // point of the animal — higher than the head.
            // The back is an ARCH, not a ramp. A gorilla's spine bows upward
            // between small, low-slung hips and the shoulder hump, so the
            // mid-back stands proud of a straight hip-to-shoulder line. The
            // pieces below are deliberately placed above that line — Loin and
            // Torso are what make the curve read from the fixed side-on
            // camera; without them the animal is a wedge, which is the
            // hunched-human silhouette again.
            AddPart(root.transform, "Rump", PrimitiveType.Sphere, new Vector3(0f, 0.46f, -0.40f), new Vector3(0.76f, 0.58f, 0.70f), fur);
            AddPart(root.transform, "Loin", PrimitiveType.Sphere, new Vector3(0f, 0.76f, -0.32f), new Vector3(0.84f, 0.66f, 0.64f), fur);
            AddPart(root.transform, "Torso", PrimitiveType.Sphere, new Vector3(0f, 0.94f, -0.08f), new Vector3(1.04f, 0.94f, 0.90f), furMid);
            AddPart(root.transform, "Chest", PrimitiveType.Sphere, new Vector3(0f, 1.04f, 0.24f), new Vector3(1.24f, 1.00f, 0.98f), furMid);
            // The saddle follows the arch, so it is long and curved along the
            // back rather than a patch sitting flat on top of it.
            AddPart(root.transform, "Saddle", PrimitiveType.Sphere, new Vector3(0f, 1.22f, -0.22f), new Vector3(0.90f, 0.50f, 0.88f), silver);
            // The hump over the shoulders — the peak of the silhouette.
            AddPart(root.transform, "Hump", PrimitiveType.Sphere, new Vector3(0f, 1.38f, 0.06f), new Vector3(1.02f, 0.54f, 0.70f), furLight);
            AddPart(root.transform, "ShoulderL", PrimitiveType.Sphere, new Vector3(-0.60f, 1.20f, 0.24f), Vector3.one * 0.56f, furLight);
            AddPart(root.transform, "ShoulderR", PrimitiveType.Sphere, new Vector3(0.60f, 1.20f, 0.24f), Vector3.one * 0.56f, furLight);
            AddPart(root.transform, "PecL", PrimitiveType.Sphere, new Vector3(-0.27f, 0.94f, 0.55f), new Vector3(0.44f, 0.40f, 0.28f), hide);
            AddPart(root.transform, "PecR", PrimitiveType.Sphere, new Vector3(0.27f, 0.94f, 0.55f), new Vector3(0.44f, 0.40f, 0.28f), hide);
            // Barely any neck: the head sits straight off the chest, slung
            // forward and low between the shoulders.
            AddPart(root.transform, "Neck", PrimitiveType.Sphere, new Vector3(0f, 1.30f, 0.34f), new Vector3(0.44f, 0.32f, 0.36f), fur);

            // Facial features parent to Head so the chest-beat head pulse
            // scales the whole face, not a bare skull sphere.
            var head = AddPart(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.38f, 0.54f), new Vector3(0.62f, 0.60f, 0.58f), furMid);
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

            // Arms hang from the FORWARD shoulders, so they fall in front of
            // the chest and plant ahead of the body rather than beside it.
            // Forearms are thicker than the upper arms — the heavy-wristed
            // taper is a big part of reading as an ape.
            AddLimb(root.transform, "ArmL", new Vector3(-0.64f, 1.22f, 0.26f), 0.18f, 0.44f, 0.19f, 0.40f, 0.21f, furMid, furLight, hide);
            AddLimb(root.transform, "ArmR", new Vector3(0.64f, 1.22f, 0.26f), 0.18f, 0.44f, 0.19f, 0.40f, 0.21f, furMid, furLight, hide);
            // Short, stocky legs tucked well back under the small hips, which
            // the arch has dropped and pushed further behind the ribcage.
            AddLimb(root.transform, "LegL", new Vector3(-0.29f, 0.50f, -0.28f), 0.19f, 0.24f, 0.17f, 0.20f, 0.18f, fur, fur, hide);
            AddLimb(root.transform, "LegR", new Vector3(0.29f, 0.50f, -0.28f), 0.19f, 0.24f, 0.17f, 0.20f, 0.18f, fur, fur, hide);

            return root;
        }

        // ---------------------------------------------------------------
        // Enemies
        // ---------------------------------------------------------------

        public static GameObject Human(HumanVariant variant = HumanVariant.Grunt, EnemyModifierRoll modifiers = default)
        {
            var root = new GameObject("HumanModel");

            var belt = new Color(0.13f, 0.10f, 0.09f);

            Color shirtBase = new Color(0.72f, 0.20f, 0.18f);
            float scale = 1f;
            float build = 1f;      // torso/limb thickness
            bool holdsWeapon = false;

            switch (variant)
            {
                case HumanVariant.Runner:
                    shirtBase = new Color(0.26f, 0.66f, 0.34f);
                    scale = 0.88f;
                    build = 0.85f;
                    break;
                case HumanVariant.Brute:
                    shirtBase = new Color(0.40f, 0.17f, 0.50f);
                    scale = 1.5f;
                    build = 1.28f;
                    break;
                case HumanVariant.Thrower:
                    shirtBase = new Color(0.82f, 0.52f, 0.16f);
                    scale = 1f;
                    holdsWeapon = true;
                    break;
                case HumanVariant.Shieldman:
                    shirtBase = new Color(0.30f, 0.36f, 0.62f);
                    scale = 1.08f;
                    build = 1.1f;
                    break;
                case HumanVariant.Bomber:
                    shirtBase = new Color(0.85f, 0.78f, 0.22f);
                    scale = 0.95f;
                    break;
                case HumanVariant.Medic:
                    shirtBase = new Color(0.92f, 0.92f, 0.94f);
                    scale = 1f;
                    build = 0.92f;
                    break;
            }

            // Everything below the type's silhouette and shirt hue is rolled
            // per individual, so a hundred-man wave reads as a crowd of
            // people rather than one man cloned a hundred times.
            var look = HumanAppearance.Roll(shirtBase);
            var skin = look.Skin;
            var pants = look.Pants;
            var shoe = look.Shoe;
            var hair = look.Hair;
            var shirt = look.Shirt;
            scale *= look.HeightScale;
            build *= look.BuildScale;

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
            BuildFace(head.transform, look, skin, hair);

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

        // Faces are assembled from the rolled appearance: eye spacing and
        // size, nose size, hairstyle and facial hair all vary. All positions
        // are in head-local space, so they follow the head's scale pulse.
        static void BuildFace(Transform head, HumanAppearance look, Color skin, Color hair)
        {
            AddPart(head, "Nose", PrimitiveType.Sphere, new Vector3(0f, -0.06f, 0.48f), Vector3.one * look.NoseSize, skin);
            AddPart(head, "EyeL", PrimitiveType.Sphere, new Vector3(-look.EyeSpacing, 0.06f, 0.40f), new Vector3(look.EyeSize, look.EyeSize * 0.88f, 0.10f), Color.black);
            AddPart(head, "EyeR", PrimitiveType.Sphere, new Vector3(look.EyeSpacing, 0.06f, 0.40f), new Vector3(look.EyeSize, look.EyeSize * 0.88f, 0.10f), Color.black);

            if (look.HeavyBrow)
            {
                AddPart(head, "Brow", PrimitiveType.Cube, new Vector3(0f, 0.22f, 0.36f), new Vector3(0.78f, 0.10f, 0.26f), hair);
            }

            switch (look.Hairstyle)
            {
                case HairStyle.Bald:
                    break;
                case HairStyle.Cropped:
                    AddPart(head, "Hair", PrimitiveType.Sphere, new Vector3(0f, 0.16f, -0.04f), new Vector3(1.04f, 0.62f, 1.06f), hair);
                    break;
                case HairStyle.Mop:
                    AddPart(head, "Hair", PrimitiveType.Sphere, new Vector3(0f, 0.22f, -0.06f), new Vector3(1.14f, 0.86f, 1.16f), hair);
                    break;
                case HairStyle.Topknot:
                    AddPart(head, "Hair", PrimitiveType.Sphere, new Vector3(0f, 0.18f, -0.06f), new Vector3(1.06f, 0.70f, 1.08f), hair);
                    AddPart(head, "Bun", PrimitiveType.Sphere, new Vector3(0f, 0.62f, -0.10f), Vector3.one * 0.42f, hair);
                    break;
                case HairStyle.Cap:
                    AddPart(head, "Cap", PrimitiveType.Sphere, new Vector3(0f, 0.20f, -0.02f), new Vector3(1.12f, 0.70f, 1.12f), look.CapColor);
                    AddPart(head, "CapPeak", PrimitiveType.Cube, new Vector3(0f, 0.12f, 0.46f), new Vector3(0.72f, 0.08f, 0.44f), look.CapColor);
                    break;
            }

            if (look.Beard)
            {
                AddPart(head, "Beard", PrimitiveType.Sphere, new Vector3(0f, -0.34f, 0.24f), new Vector3(0.86f, 0.60f, 0.82f), hair);
            }
            else if (look.Moustache)
            {
                AddPart(head, "Moustache", PrimitiveType.Cube, new Vector3(0f, -0.20f, 0.44f), new Vector3(0.44f, 0.09f, 0.16f), hair);
            }
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

            // Stone colour varies per rock: cool grey, warm sandstone, or
            // dark basalt, with a little jitter on top.
            Color baseTone;
            float tint = Random.value;
            if (tint < 0.55f) baseTone = new Color(0.46f, 0.45f, 0.43f);
            else if (tint < 0.82f) baseTone = new Color(0.52f, 0.46f, 0.38f);
            else baseTone = new Color(0.34f, 0.34f, 0.36f);

            float v = Random.Range(-0.05f, 0.05f);
            var gray = new Color(baseTone.r + v, baseTone.g + v, baseTone.b + v);
            var grayDark = gray * 0.72f;
            var grayLight = Color.Lerp(gray, Color.white, 0.18f);

            int shape = Random.Range(0, 3);
            if (shape == 0)
            {
                // Angular boulder.
                var core = AddPart(root.transform, "Core", PrimitiveType.Cube, new Vector3(0f, 0.33f, 0f), new Vector3(0.80f, 0.56f, 0.72f), gray);
                core.transform.localRotation = Quaternion.Euler(8f, 24f, 6f);
                var chunk = AddPart(root.transform, "Chunk", PrimitiveType.Cube, new Vector3(0.26f, 0.48f, 0.08f), new Vector3(0.44f, 0.40f, 0.42f), grayLight);
                chunk.transform.localRotation = Quaternion.Euler(-14f, 40f, 18f);
                var chunk2 = AddPart(root.transform, "Chunk2", PrimitiveType.Cube, new Vector3(-0.28f, 0.36f, -0.12f), new Vector3(0.42f, 0.34f, 0.38f), grayDark);
                chunk2.transform.localRotation = Quaternion.Euler(12f, -30f, -10f);
                AddPart(root.transform, "Pebble", PrimitiveType.Sphere, new Vector3(0.34f, 0.10f, -0.26f), new Vector3(0.24f, 0.18f, 0.22f), grayDark);
            }
            else if (shape == 1)
            {
                // Low flat slab, like a weathered outcrop.
                var slab = AddPart(root.transform, "Slab", PrimitiveType.Cube, new Vector3(0f, 0.20f, 0f), new Vector3(1.10f, 0.30f, 0.86f), gray);
                slab.transform.localRotation = Quaternion.Euler(4f, 18f, -3f);
                var ledge = AddPart(root.transform, "Ledge", PrimitiveType.Cube, new Vector3(-0.18f, 0.38f, 0.10f), new Vector3(0.62f, 0.22f, 0.54f), grayLight);
                ledge.transform.localRotation = Quaternion.Euler(-6f, 34f, 5f);
                AddPart(root.transform, "Chip", PrimitiveType.Sphere, new Vector3(0.44f, 0.12f, -0.20f), new Vector3(0.30f, 0.20f, 0.26f), grayDark);
            }
            else
            {
                // Cluster of rounded stones.
                AddPart(root.transform, "StoneA", PrimitiveType.Sphere, new Vector3(0f, 0.28f, 0f), new Vector3(0.70f, 0.52f, 0.66f), gray);
                AddPart(root.transform, "StoneB", PrimitiveType.Sphere, new Vector3(0.36f, 0.20f, 0.18f), new Vector3(0.46f, 0.38f, 0.44f), grayLight);
                AddPart(root.transform, "StoneC", PrimitiveType.Sphere, new Vector3(-0.32f, 0.18f, -0.14f), new Vector3(0.42f, 0.34f, 0.40f), grayDark);
                AddPart(root.transform, "StoneD", PrimitiveType.Sphere, new Vector3(0.06f, 0.14f, -0.38f), new Vector3(0.34f, 0.26f, 0.32f), gray);
            }

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
            if (species < 0) species = Random.Range(0, 5);

            // Bark and foliage are tinted per tree so a stand of the same
            // species doesn't look stamped out.
            float barkShift = Random.Range(-0.05f, 0.05f);
            var bark = new Color(0.34f + barkShift, 0.24f + barkShift * 0.7f, 0.17f + barkShift * 0.5f);
            var barkDark = bark * 0.72f;
            float leafShift = Random.Range(-0.05f, 0.06f);

            switch (species)
            {
                case 0: // Broad canopy
                {
                    var leaves = new Color(0.20f, 0.42f + leafShift, 0.20f);
                    var leavesLight = new Color(0.27f, 0.51f + leafShift, 0.24f);
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
                    var needle = new Color(0.15f, 0.34f + leafShift, 0.22f);
                    var needleLight = new Color(0.20f, 0.42f + leafShift, 0.26f);
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
                case 3: // Bare dead tree — bark only, stark silhouette
                {
                    AddPart(root.transform, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 1.05f, 0f), new Vector3(0.26f, 1.05f, 0.26f), barkDark);
                    AddPart(root.transform, "Flare", PrimitiveType.Cylinder, new Vector3(0f, 0.12f, 0f), new Vector3(0.40f, 0.14f, 0.40f), barkDark);
                    for (int i = 0; i < 5; i++)
                    {
                        float angle = Random.Range(0f, 360f);
                        float height = Random.Range(1.3f, 2.2f);
                        var branch = AddPart(root.transform, "Branch" + i, PrimitiveType.Capsule, Vector3.zero, new Vector3(0.10f, 0.34f, 0.10f), bark);
                        branch.transform.localPosition = new Vector3(0f, height, 0f) + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.34f, 0.12f, 0f);
                        branch.transform.localRotation = Quaternion.Euler(0f, angle, Random.Range(38f, 66f));
                    }
                    break;
                }
                case 4: // Pale-trunked birch with a light, airy crown
                {
                    var birchBark = new Color(0.82f, 0.80f, 0.74f);
                    var leaves = new Color(0.42f, 0.58f + leafShift, 0.24f);
                    var leavesLight = new Color(0.52f, 0.66f + leafShift, 0.30f);
                    AddPart(root.transform, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 1.25f, 0f), new Vector3(0.20f, 1.25f, 0.20f), birchBark);
                    for (int i = 0; i < 3; i++)
                    {
                        AddPart(root.transform, "Band" + i, PrimitiveType.Cylinder, new Vector3(0f, 0.5f + i * 0.7f, 0f), new Vector3(0.21f, 0.04f, 0.21f), new Color(0.28f, 0.26f, 0.24f));
                    }
                    AddPart(root.transform, "CrownA", PrimitiveType.Sphere, new Vector3(0f, 2.55f, 0f), new Vector3(0.98f, 0.86f, 0.98f), leaves);
                    AddPart(root.transform, "CrownB", PrimitiveType.Sphere, new Vector3(0.34f, 2.25f, 0.20f), Vector3.one * 0.62f, leavesLight);
                    AddPart(root.transform, "CrownC", PrimitiveType.Sphere, new Vector3(-0.30f, 2.34f, -0.18f), Vector3.one * 0.56f, leaves);
                    break;
                }
                default: // Tall palm-ish with a leaning trunk
                {
                    var frond = new Color(0.24f, 0.47f + leafShift, 0.22f);
                    var frondDark = new Color(0.18f, 0.38f + leafShift, 0.18f);
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

        // A fallen log — breaks up open ground and reads as forest floor.
        public static GameObject Log()
        {
            var root = new GameObject("Log");
            var bark = new Color(0.32f + Random.Range(-0.04f, 0.04f), 0.23f, 0.16f);
            var inner = new Color(0.60f, 0.47f, 0.31f);

            var trunk = AddPart(root.transform, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 0.22f, 0f), new Vector3(0.44f, 0.80f, 0.44f), bark);
            trunk.transform.localRotation = Quaternion.Euler(90f, 0f, Random.Range(-8f, 8f));
            AddPart(root.transform, "CutEnd", PrimitiveType.Cylinder, new Vector3(0f, 0.22f, 0.80f), new Vector3(0.40f, 0.03f, 0.40f), inner);
            AddPart(root.transform, "Knot", PrimitiveType.Sphere, new Vector3(0.16f, 0.36f, -0.18f), new Vector3(0.22f, 0.16f, 0.22f), bark);
            if (Random.value < 0.5f)
            {
                AddPart(root.transform, "Moss", PrimitiveType.Sphere, new Vector3(-0.08f, 0.40f, 0.22f), new Vector3(0.34f, 0.14f, 0.5f), new Color(0.25f, 0.44f, 0.22f));
            }
            return root;
        }

        // Tall ferny clump, a taller counterpart to the grass tufts.
        public static GameObject Fern()
        {
            var root = new GameObject("Fern");
            var a = new Color(0.20f, 0.42f + Random.Range(-0.05f, 0.05f), 0.20f);
            var b = new Color(0.26f, 0.50f, 0.24f);

            int fronds = Random.Range(5, 8);
            for (int i = 0; i < fronds; i++)
            {
                float angle = i * (360f / fronds) + Random.Range(-12f, 12f);
                float len = Random.Range(0.30f, 0.48f);
                var frond = AddPart(root.transform, "Frond" + i, PrimitiveType.Capsule, Vector3.zero, new Vector3(0.11f, len, 0.11f), i % 2 == 0 ? a : b);
                frond.transform.localPosition = Quaternion.Euler(0f, angle, 0f) * new Vector3(0.14f, len * 0.75f, 0f);
                frond.transform.localRotation = Quaternion.Euler(0f, angle, Random.Range(30f, 52f));
            }
            DisableShadowCasting(root);
            return root;
        }

        // Scatter of small stones — ground detail with no gameplay meaning,
        // distinct from the attackable boulders.
        public static GameObject Pebbles()
        {
            var root = new GameObject("Pebbles");
            var tone = new Color(0.48f, 0.47f, 0.45f);

            int count = Random.Range(3, 6);
            for (int i = 0; i < count; i++)
            {
                var dir = Random.insideUnitCircle * 0.35f;
                float s = Random.Range(0.09f, 0.18f);
                var shade = tone * Random.Range(0.8f, 1.15f);
                AddPart(root.transform, "Pebble" + i, PrimitiveType.Sphere, new Vector3(dir.x, s * 0.4f, dir.y), new Vector3(s, s * 0.6f, s * 0.9f), shade);
            }
            DisableShadowCasting(root);
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

        // A live bomb left behind by a killed Bomber. Deliberately a cartoon
        // black sphere with a lit fuse: the player needs to read "there is a
        // bomb there" from across the arena, and the shape does that faster
        // than the warning circle under it.
        public static GameObject Bomb()
        {
            var root = new GameObject("Bomb");

            var body = AddPart(root.transform, "Body", PrimitiveType.Sphere, new Vector3(0f, 0.34f, 0f),
                Vector3.one * 0.62f, new Color(0.12f, 0.12f, 0.14f));
            AddPart(body.transform, "Sheen", PrimitiveType.Sphere, new Vector3(-0.22f, 0.24f, -0.22f),
                Vector3.one * 0.26f, new Color(0.34f, 0.34f, 0.38f));
            AddPart(root.transform, "Collar", PrimitiveType.Cylinder, new Vector3(0f, 0.62f, 0f),
                new Vector3(0.20f, 0.06f, 0.20f), new Color(0.44f, 0.36f, 0.20f));

            var fuse = AddPart(root.transform, "Fuse", PrimitiveType.Cylinder, new Vector3(0.07f, 0.76f, 0.03f),
                new Vector3(0.06f, 0.13f, 0.06f), new Color(0.52f, 0.44f, 0.30f));
            fuse.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);

            var spark = CreateBarePrimitive(PrimitiveType.Sphere, "Spark", root.transform);
            spark.transform.localPosition = new Vector3(0.13f, 0.90f, 0.03f);
            spark.transform.localScale = Vector3.one * 0.16f;
            spark.GetComponent<MeshRenderer>().sharedMaterial = MaterialCache.GetGlowing(new Color(1f, 0.78f, 0.25f), 2.4f);

            DisableShadowCasting(root);
            return root;
        }

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
