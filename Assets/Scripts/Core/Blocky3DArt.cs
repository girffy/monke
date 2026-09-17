using UnityEngine;

namespace GorillaSurvivors.Core
{
    public enum HumanVariant { Grunt, Runner, Brute, Thrower, Shieldman, Bomber, Medic, Wizard, Ogre }

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

        // How far the gorilla model has to be lifted to stand ON the ground
        // rather than in it. The build below hangs its limbs well below the
        // model origin — knuckles about 0.30 down, feet about 0.23 — and the
        // knuckle-walk crouch drops it another 0.16 on top, so placed at y=0
        // the animal is buried to the shins. Enemies don't need this; their
        // feet already land at the origin.
        //
        // Split the difference between knuckles and feet: the limb ends are
        // spheres, so a couple of centimetres either way disappears into the
        // curve.
        public const float GorillaGroundLift = 0.17f;

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

            // The topline is a SWAY-BACK, and the order of the heights below
            // is the entire silhouette. Reading from the tail forward, the
            // top surface of each piece goes:
            //
            //   rump 1.15  ->  loin 1.09  ->  mid 1.25  ->  chest 1.42
            //   ->  shoulders 1.50  ->  HEAD 1.72
            //
            // So the butt is a local high point, the back dips behind it, and
            // then arches up to the head, which is the highest thing on the
            // animal. Three earlier passes each put the peak somewhere in the
            // middle of the back — over the hips, then over the shoulders —
            // and every one of them read as a hunchback, because a lump
            // anywhere along the spine IS a hunch no matter how the line
            // curves into it. The peak has to be the head.
            //
            // The shoulders sit beside the head and well forward, which is
            // what carries the ape's weight onto its knuckles.
            AddPart(root.transform, "Rump", PrimitiveType.Sphere, new Vector3(0f, 0.82f, -0.52f), new Vector3(0.76f, 0.66f, 0.68f), fur);
            AddPart(root.transform, "Loin", PrimitiveType.Sphere, new Vector3(0f, 0.80f, -0.18f), new Vector3(0.86f, 0.58f, 0.72f), fur);
            AddPart(root.transform, "Torso", PrimitiveType.Sphere, new Vector3(0f, 0.92f, 0.06f), new Vector3(1.00f, 0.66f, 0.78f), furMid);
            AddPart(root.transform, "Chest", PrimitiveType.Sphere, new Vector3(0f, 1.04f, 0.30f), new Vector3(1.22f, 0.76f, 0.92f), furMid);
            // The silver saddle sits over the dip and the rump — the part of
            // the back that is actually facing the sky.
            AddPart(root.transform, "Saddle", PrimitiveType.Sphere, new Vector3(0f, 0.98f, -0.34f), new Vector3(0.84f, 0.44f, 0.86f), silver);
            // Beside the head, not behind it.
            AddPart(root.transform, "ShoulderL", PrimitiveType.Sphere, new Vector3(-0.56f, 1.22f, 0.36f), Vector3.one * 0.56f, furLight);
            AddPart(root.transform, "ShoulderR", PrimitiveType.Sphere, new Vector3(0.56f, 1.22f, 0.36f), Vector3.one * 0.56f, furLight);
            AddPart(root.transform, "PecL", PrimitiveType.Sphere, new Vector3(-0.26f, 0.94f, 0.64f), new Vector3(0.42f, 0.38f, 0.28f), hide);
            AddPart(root.transform, "PecR", PrimitiveType.Sphere, new Vector3(0.26f, 0.94f, 0.64f), new Vector3(0.42f, 0.38f, 0.28f), hide);
            AddPart(root.transform, "Neck", PrimitiveType.Sphere, new Vector3(0f, 1.26f, 0.48f), new Vector3(0.48f, 0.36f, 0.38f), fur);

            // Facial features parent to Head so the chest-beat head pulse
            // scales the whole face, not a bare skull sphere.
            // The highest point on the animal.
            var head = AddPart(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.42f, 0.58f), new Vector3(0.62f, 0.60f, 0.58f), furMid);
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
            // Long arms hanging from the high forward shoulders. They have to
            // be this long: the shoulders sit up beside the head now, and the
            // knuckles still have to reach the ground.
            AddLimb(root.transform, "ArmL", new Vector3(-0.60f, 1.18f, 0.36f), 0.19f, 0.50f, 0.20f, 0.46f, 0.22f, furMid, furLight, hide);
            AddLimb(root.transform, "ArmR", new Vector3(0.60f, 1.18f, 0.36f), 0.19f, 0.50f, 0.20f, 0.46f, 0.22f, furMid, furLight, hide);
            // Short legs under the high rump, so the hind end stands tall on
            // stubby limbs while the front is long-armed — the proportion
            // that makes the sway-back read.
            AddLimb(root.transform, "LegL", new Vector3(-0.30f, 0.72f, -0.46f), 0.20f, 0.28f, 0.18f, 0.24f, 0.19f, fur, fur, hide);
            AddLimb(root.transform, "LegR", new Vector3(0.30f, 0.72f, -0.46f), 0.20f, 0.28f, 0.18f, 0.24f, 0.19f, fur, fur, hide);

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
                    // WIDE, not tall. This is the round-one heavy, and a man
                    // half again the height of everyone else read as a boss —
                    // far too imposing for the thing you meet in the first
                    // wave. The threat is legible from bulk alone: barely
                    // taller than a grunt, but half again as broad.
                    shirtBase = new Color(0.42f, 0.40f, 0.44f);
                    scale = 1.12f;
                    build = 1.34f;
                    break;
                case HumanVariant.Ogre:
                    // The old Brute, kept for what its height was always
                    // right for: a late-game wall that hits like a truck.
                    shirtBase = new Color(0.40f, 0.17f, 0.50f);
                    scale = 1.55f;
                    build = 1.34f;
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
                case HumanVariant.Wizard:
                    // Deep violet, the only robe in the game — the wizard has
                    // to be identifiable across the arena the instant it
                    // arrives, because it is the one enemy you must deal with
                    // on its terms rather than yours.
                    shirtBase = new Color(0.24f, 0.14f, 0.42f);
                    scale = 1.12f;
                    build = 0.95f;
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

            // The body is a taper, not a tube: narrow hips, a chest that is
            // wider than the waist, and shoulders capping the top. A single
            // capsule for the whole torso made every man read as a bollard,
            // and it is the one shape all eight variants share, so it is
            // worth the extra three spheres.
            AddPart(root.transform, "Hips", PrimitiveType.Sphere, new Vector3(0f, 0.72f, 0f), new Vector3(0.36f * build, 0.26f, 0.27f * build), pants);
            AddPart(root.transform, "Waist", PrimitiveType.Capsule, new Vector3(0f, 0.92f, 0f), new Vector3(0.38f * build, 0.16f, 0.27f * build), shirt);
            AddPart(root.transform, "Chest", PrimitiveType.Capsule, new Vector3(0f, 1.10f, 0f), new Vector3(0.47f * build, 0.19f, 0.32f * build), shirt);
            AddPart(root.transform, "Belt", PrimitiveType.Cube, new Vector3(0f, 0.80f, 0f), new Vector3(0.44f * build, 0.07f, 0.31f * build), belt);

            // Shoulder caps, so the arms grow out of a body instead of being
            // stuck to its sides.
            AddPart(root.transform, "ShoulderL", PrimitiveType.Sphere, new Vector3(-0.25f * build, 1.20f, 0f), new Vector3(0.20f * build, 0.17f, 0.20f * build), shirt);
            AddPart(root.transform, "ShoulderR", PrimitiveType.Sphere, new Vector3(0.25f * build, 1.20f, 0f), new Vector3(0.20f * build, 0.17f, 0.20f * build), shirt);
            AddPart(root.transform, "Collar", PrimitiveType.Capsule, new Vector3(0f, 1.24f, 0f), new Vector3(0.30f * build, 0.05f, 0.22f * build), shirt);
            AddPart(root.transform, "Neck", PrimitiveType.Capsule, new Vector3(0f, 1.29f, 0f), new Vector3(0.13f, 0.06f, 0.13f), skin);

            var head = AddPart(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.44f, 0f), new Vector3(0.30f, 0.34f, 0.30f), skin);
            AddPart(head.transform, "Ears", PrimitiveType.Sphere, new Vector3(0f, -0.06f, -0.04f), new Vector3(1.14f, 0.34f, 0.72f), skin);
            BuildFace(head.transform, look, skin, hair);

            AddLimb(root.transform, "ArmL", new Vector3(-0.27f * build, 1.19f, 0f), 0.085f * build, 0.26f, 0.075f * build, 0.24f, 0.09f, shirt, skin, skin);
            AddLimb(root.transform, "ArmR", new Vector3(0.27f * build, 1.19f, 0f), 0.085f * build, 0.26f, 0.075f * build, 0.24f, 0.09f, shirt, skin, skin);

            if (holdsWeapon)
            {
                var weapon = AddPart(root.transform, "Weapon", PrimitiveType.Capsule, new Vector3(0.38f, 0.82f, 0.18f), new Vector3(0.07f, 0.26f, 0.07f), weaponColor);
                weapon.transform.localRotation = Quaternion.Euler(62f, 0f, 18f);
                AddPart(weapon.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.05f, 0f), new Vector3(1.9f, 0.55f, 1.9f), weaponColor);
            }

            BuildVariantProps(root, head.transform, variant, build, skin, hair, shirt);
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
        // Anything worn on the head parents to the HEAD, not the body. Two
        // of these used to hang off the root at a hard-coded height: at
        // default build they happened to land near the face, and at any
        // other height or scale they floated in front of it — the Brute's
        // brow ridge in particular read as a pair of sunglasses hovering a
        // few centimetres off his nose.
        static void BuildVariantProps(GameObject root, Transform head, HumanVariant variant, float build,
            Color skin, Color hair, Color shirt)
        {
            switch (variant)
            {
                case HumanVariant.Shieldman:
                {
                    var steel = new Color(0.52f, 0.54f, 0.58f);
                    var steelDark = new Color(0.36f, 0.37f, 0.40f);
                    var steelLight = new Color(0.70f, 0.72f, 0.76f);

                    // Big slab held out front — the visual promise that
                    // frontal hits get soaked.
                    var shield = AddPart(root.transform, "Shield", PrimitiveType.Cube, new Vector3(0f, 1.02f, 0.44f), new Vector3(0.80f, 0.92f, 0.09f), steel);
                    AddPart(shield.transform, "Boss", PrimitiveType.Sphere, new Vector3(0f, 0f, -1.1f), new Vector3(0.36f, 0.32f, 1.6f), steelLight);
                    AddPart(shield.transform, "RimTop", PrimitiveType.Cube, new Vector3(0f, 0.46f, 0f), new Vector3(1.06f, 0.10f, 1.5f), steelDark);
                    AddPart(shield.transform, "RimBottom", PrimitiveType.Cube, new Vector3(0f, -0.46f, 0f), new Vector3(1.06f, 0.10f, 1.5f), steelDark);
                    AddPart(shield.transform, "Spine", PrimitiveType.Cube, new Vector3(0f, 0f, -0.6f), new Vector3(0.14f, 0.92f, 0.9f), steelDark);

                    // A helmet makes the "armoured" read carry from behind
                    // too, where the shield is hidden.
                    AddPart(head, "Helm", PrimitiveType.Sphere, new Vector3(0f, 0.18f, -0.02f), new Vector3(1.16f, 0.82f, 1.16f), steel);
                    AddPart(head, "HelmRidge", PrimitiveType.Cube, new Vector3(0f, 0.50f, -0.02f), new Vector3(0.12f, 0.24f, 1.05f), steelLight);
                    AddPart(head, "Nasal", PrimitiveType.Cube, new Vector3(0f, 0.04f, 0.46f), new Vector3(0.12f, 0.58f, 0.14f), steel);
                    break;
                }
                case HumanVariant.Bomber:
                {
                    // Live charge strapped to the chest, fuse and all.
                    var bomb = AddPart(root.transform, "Bomb", PrimitiveType.Sphere, new Vector3(0f, 1.02f, 0.30f), Vector3.one * 0.34f, new Color(0.16f, 0.16f, 0.18f));
                    AddPart(bomb.transform, "Fuse", PrimitiveType.Capsule, new Vector3(0f, 0.62f, 0f), new Vector3(0.14f, 0.28f, 0.14f), new Color(0.62f, 0.52f, 0.34f));
                    AddGlowPart(bomb.transform, "Spark", PrimitiveType.Sphere, new Vector3(0f, 1.05f, 0f), Vector3.one * 0.26f, new Color(1f, 0.62f, 0.15f));

                    // Straps over both shoulders, so the charge reads as
                    // deliberately worn rather than stuck on.
                    var strap = new Color(0.30f, 0.25f, 0.18f);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var band = AddPart(root.transform, "Strap", PrimitiveType.Cube, new Vector3(s * 0.11f, 1.12f, 0.16f), new Vector3(0.07f, 0.36f, 0.20f), strap);
                        band.transform.localRotation = Quaternion.Euler(0f, 0f, s * 14f);
                    }
                    break;
                }
                case HumanVariant.Medic:
                {
                    var red = new Color(0.85f, 0.18f, 0.18f);

                    // Red cross on the chest and a satchel on the hip.
                    AddPart(root.transform, "CrossV", PrimitiveType.Cube, new Vector3(0f, 1.05f, 0.17f), new Vector3(0.09f, 0.26f, 0.04f), red);
                    AddPart(root.transform, "CrossH", PrimitiveType.Cube, new Vector3(0f, 1.05f, 0.17f), new Vector3(0.26f, 0.09f, 0.04f), red);

                    var satchel = AddPart(root.transform, "Satchel", PrimitiveType.Cube, new Vector3(0.27f, 0.80f, -0.04f), new Vector3(0.22f, 0.20f, 0.15f), new Color(0.55f, 0.48f, 0.38f));
                    AddPart(satchel.transform, "Clasp", PrimitiveType.Cube, new Vector3(0f, 0.1f, 0.55f), new Vector3(0.5f, 0.35f, 0.2f), new Color(0.38f, 0.32f, 0.24f));
                    var sling = AddPart(root.transform, "Sling", PrimitiveType.Cube, new Vector3(0.10f, 1.06f, 0.02f), new Vector3(0.07f, 0.42f, 0.30f), new Color(0.48f, 0.42f, 0.33f));
                    sling.transform.localRotation = Quaternion.Euler(0f, 0f, 20f);

                    // A white cap with a cross on it, rather than the cross
                    // painted straight onto the forehead — at head scale a
                    // small red mark on bare skin reads as a head wound, not
                    // as insignia. The medic still has to be findable in a
                    // crowd from any angle, hence putting it up top.
                    AddPart(head, "Cap", PrimitiveType.Sphere, new Vector3(0f, 0.20f, -0.02f), new Vector3(1.14f, 0.72f, 1.14f), new Color(0.94f, 0.94f, 0.96f));
                    AddPart(head, "CapPeak", PrimitiveType.Cube, new Vector3(0f, 0.12f, 0.46f), new Vector3(0.70f, 0.08f, 0.42f), new Color(0.94f, 0.94f, 0.96f));
                    AddPart(head, "CapCrossH", PrimitiveType.Cube, new Vector3(0f, 0.46f, 0.06f), new Vector3(0.44f, 0.16f, 0.14f), red);
                    AddPart(head, "CapCrossV", PrimitiveType.Cube, new Vector3(0f, 0.46f, 0.06f), new Vector3(0.14f, 0.16f, 0.44f), red);
                    break;
                }
                case HumanVariant.Ogre:
                {
                    var leather = new Color(0.27f, 0.20f, 0.16f);

                    // Slabs of shoulder and a heavy jaw: the bulk has to read
                    // in silhouette, since an Ogre is mostly just a bigger
                    // version of the same body.
                    for (int s = -1; s <= 1; s += 2)
                    {
                        AddPart(root.transform, "Pauldron", PrimitiveType.Sphere,
                            new Vector3(s * 0.30f * build, 1.26f, 0f), new Vector3(0.30f * build, 0.20f, 0.30f * build), leather);
                        // Wrapped knuckles on the ends of both arms.
                        var arm = root.transform.Find(s < 0 ? "ArmL" : "ArmR");
                        var fist = arm != null ? arm.Find("Lower/End") : null;
                        if (fist != null)
                        {
                            AddPart(fist.parent, "Wrap", PrimitiveType.Sphere,
                                fist.localPosition + new Vector3(0f, 0.04f, 0f), new Vector3(0.13f, 0.07f, 0.13f), leather);
                        }
                    }

                    AddPart(head, "Jaw", PrimitiveType.Cube, new Vector3(0f, -0.34f, 0.16f), new Vector3(0.80f, 0.26f, 0.72f), skin);
                    AddPart(head, "BrowRidge", PrimitiveType.Cube, new Vector3(0f, 0.20f, 0.34f), new Vector3(0.86f, 0.14f, 0.32f), hair);
                    break;
                }
                case HumanVariant.Brute:
                {
                    var hide = new Color(0.24f, 0.22f, 0.21f);
                    var iron = new Color(0.46f, 0.47f, 0.50f);

                    // Everything here pushes width, because width is the only
                    // thing separating this from a grunt at a glance — it is
                    // barely taller than one. A barrel chest and a gut past
                    // the belt, shoulders out past the arms, and no neck.
                    AddPart(root.transform, "Barrel", PrimitiveType.Sphere,
                        new Vector3(0f, 1.06f, 0.02f), new Vector3(0.54f * build, 0.33f, 0.42f * build), shirt);
                    AddPart(root.transform, "Gut", PrimitiveType.Sphere,
                        new Vector3(0f, 0.86f, 0.04f), new Vector3(0.46f * build, 0.23f, 0.38f * build), shirt);
                    AddPart(root.transform, "Trapezius", PrimitiveType.Capsule,
                        new Vector3(0f, 1.28f, -0.02f), new Vector3(0.40f * build, 0.10f, 0.25f * build), shirt);

                    for (int s = -1; s <= 1; s += 2)
                    {
                        AddPart(root.transform, "Pauldron", PrimitiveType.Sphere,
                            new Vector3(s * 0.30f * build, 1.22f, 0f),
                            new Vector3(0.29f * build, 0.24f, 0.31f * build), hide);
                        AddPart(root.transform, "PauldronStud", PrimitiveType.Sphere,
                            new Vector3(s * 0.35f * build, 1.27f, 0f),
                            new Vector3(0.09f, 0.075f, 0.09f), iron);

                        var arm = root.transform.Find(s < 0 ? "ArmL" : "ArmR");
                        var fist = arm != null ? arm.Find("Lower/End") : null;
                        if (fist != null)
                        {
                            AddPart(fist.parent, "Wrap", PrimitiveType.Sphere,
                                fist.localPosition + new Vector3(0f, 0.03f, 0f), new Vector3(0.17f, 0.10f, 0.17f), hide);
                        }
                    }

                    // A wide leather band across the middle, which reads as a
                    // belt straining rather than as a waist.
                    AddPart(root.transform, "Girdle", PrimitiveType.Cube,
                        new Vector3(0f, 0.78f, 0f), new Vector3(0.50f * build, 0.13f, 0.40f * build), hide);
                    AddPart(root.transform, "Buckle", PrimitiveType.Cube,
                        new Vector3(0f, 0.78f, 0.21f * build), new Vector3(0.14f, 0.11f, 0.06f), iron);

                    AddPart(head, "Jaw", PrimitiveType.Cube, new Vector3(0f, -0.32f, 0.16f), new Vector3(0.92f, 0.30f, 0.78f), skin);
                    AddPart(head, "BrowRidge", PrimitiveType.Cube, new Vector3(0f, 0.18f, 0.32f), new Vector3(0.94f, 0.18f, 0.34f), hair);
                    break;
                }
                case HumanVariant.Runner:
                {
                    AddPart(head, "Headband", PrimitiveType.Cube, new Vector3(0f, 0.22f, 0f), new Vector3(1.08f, 0.16f, 1.10f), new Color(0.90f, 0.30f, 0.30f));
                    AddPart(head, "BandTail", PrimitiveType.Cube, new Vector3(-0.34f, 0.16f, -0.36f), new Vector3(0.14f, 0.10f, 0.42f), new Color(0.82f, 0.26f, 0.26f));
                    break;
                }
                case HumanVariant.Wizard:
                {
                    var robe = new Color(0.24f, 0.14f, 0.42f);
                    var robeTrim = new Color(0.62f, 0.52f, 0.20f);
                    var arcane = new Color(0.55f, 0.85f, 1f);

                    // A robe that falls to the floor, hiding the legs: the
                    // silhouette is a cone, which reads as "not one of the
                    // men" from any distance.
                    var skirt = AddPart(root.transform, "Robe", PrimitiveType.Cylinder, new Vector3(0f, 0.44f, 0f), new Vector3(0.62f, 0.44f, 0.62f), robe);
                    AddPart(skirt.transform, "Hem", PrimitiveType.Cylinder, new Vector3(0f, -0.95f, 0f), new Vector3(1.22f, 0.10f, 1.22f), robeTrim);
                    AddPart(root.transform, "Mantle", PrimitiveType.Sphere, new Vector3(0f, 1.18f, 0f), new Vector3(0.52f, 0.26f, 0.44f), robe);

                    // Pointed hat, wide brim.
                    AddPart(head, "HatBrim", PrimitiveType.Cylinder, new Vector3(0f, 0.34f, 0f), new Vector3(1.95f, 0.05f, 1.95f), robe);
                    AddPart(head, "HatBand", PrimitiveType.Cylinder, new Vector3(0f, 0.42f, 0f), new Vector3(1.26f, 0.06f, 1.26f), robeTrim);

                    // Unity has no cone primitive, so the point is stacked
                    // out of narrowing drums — a single cylinder reads as a
                    // top hat, which is the wrong wizard entirely. Each tier
                    // leans slightly further back for a bit of droop.
                    float[] widths = { 1.14f, 0.92f, 0.70f, 0.48f, 0.26f };
                    for (int i = 0; i < widths.Length; i++)
                    {
                        float y = 0.50f + i * 0.30f;
                        AddPart(head, "HatTier" + i, PrimitiveType.Cylinder,
                            new Vector3(0f, y, -0.04f * i), new Vector3(widths[i], 0.16f, widths[i]), robe);
                    }
                    AddPart(head, "HatTip", PrimitiveType.Sphere, new Vector3(0f, 1.78f, -0.20f), Vector3.one * 0.20f, robeTrim);

                    // Long white beard.
                    AddPart(head, "Beard", PrimitiveType.Sphere, new Vector3(0f, -0.52f, 0.26f), new Vector3(0.78f, 0.86f, 0.70f), new Color(0.88f, 0.88f, 0.90f));

                    // Staff with a lit head — the thing that throws fire.
                    var staff = AddPart(root.transform, "Staff", PrimitiveType.Capsule, new Vector3(0.34f, 1.00f, 0.12f), new Vector3(0.06f, 0.62f, 0.06f), new Color(0.36f, 0.26f, 0.16f));
                    AddGlowPart(staff.transform, "Orb", PrimitiveType.Sphere, new Vector3(0f, 1.02f, 0f), new Vector3(3.2f, 0.32f, 3.2f), arcane);
                    break;
                }
                case HumanVariant.Thrower:
                {
                    // A pouch of rocks on the hip, so there is a visible
                    // source for what he keeps throwing.
                    var pouch = AddPart(root.transform, "Pouch", PrimitiveType.Sphere, new Vector3(-0.26f, 0.78f, -0.06f), new Vector3(0.22f, 0.22f, 0.18f), new Color(0.44f, 0.36f, 0.26f));
                    AddPart(pouch.transform, "Stone", PrimitiveType.Sphere, new Vector3(0.1f, 0.55f, 0f), Vector3.one * 0.45f, new Color(0.48f, 0.47f, 0.45f));
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

        // The grass sits a few centimetres BELOW y=0, which is where
        // everything else in the game lives: feet, prop bases, and the
        // arena's sand slab. That gap is what lets the sand's top sit at
        // exactly 0 without z-fighting against the grass underneath it.
        // Small enough that props standing at y=0 on the grass outside the
        // arena don't read as floating.
        public const float GroundY = -0.04f;

        public static GameObject Ground(float size = 220f)
        {
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Ground";
            Object.Destroy(plane.GetComponent<Collider>());
            plane.transform.position = new Vector3(0f, GroundY, 0f);
            plane.transform.localScale = Vector3.one * (size / 10f); // default Plane is 10x10 units

            // One tile every ~4 world units: close enough to give the surface
            // texture at player scale, far enough not to shimmer at distance.
            float tiles = size / 4f;
            plane.GetComponent<MeshRenderer>().sharedMaterial =
                MaterialCache.GetTextured(ProceduralTextures.Grass(), Color.white, new Vector2(tiles, tiles));
            return plane;
        }

        // A ring of hills far outside the arena. The world beyond the walls
        // was a flat green plane meeting a flat blue sky in a hard line, and
        // the fog only softened it — there was nothing out there to soften.
        // These sit past the fog's far distance so they read as haze-blue
        // scenery rather than as objects, and they never move, so the cost
        // is a few dozen static meshes.
        public static GameObject Backdrop(float innerRadius = 52f)
        {
            var root = new GameObject("Backdrop");
            var rng = new System.Random(4417);

            // Two overlapping rings, so there is a near range of hills with
            // a paler one behind it rather than a single hedge.
            int count = 64;
            for (int i = 0; i < count; i++)
            {
                float angle = i * 360f / count + (float)rng.NextDouble() * 6f;
                float distance = innerRadius + (float)rng.NextDouble() * 46f;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

                // Tall enough to actually clear the arena's stands from the
                // player's eye line — the first pass sat below them and was
                // invisible from inside the ring, which is the only place
                // anyone ever looks from.
                float height = 14f + (float)rng.NextDouble() * 26f;
                float width = height * (1.5f + (float)rng.NextDouble() * 1.1f);

                // Distant hills read cooler and paler the further off they
                // are — cheap aerial perspective, and it keeps the ring from
                // looking like a wall of identical lumps.
                float haze = Mathf.InverseLerp(innerRadius, innerRadius + 46f, distance);
                var near = new Color(0.26f, 0.34f, 0.25f);
                var far = new Color(0.44f, 0.52f, 0.56f);
                var tone = Color.Lerp(near, far, haze * 0.85f);

                var hill = CreateBarePrimitive(PrimitiveType.Sphere, "Hill" + i, root.transform);
                hill.transform.position = dir * distance - Vector3.up * height * 0.45f;
                hill.transform.localScale = new Vector3(width, height, width);
                var mr = hill.GetComponent<MeshRenderer>();
                mr.sharedMaterial = MaterialCache.Get(tone);
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }

            return root;
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

        // A standing stone column, the arena's version of a tree: something
        // to break line of sight and shove enemies into, and something the
        // gorilla can knock over onto a crowd. Trees in a colosseum never
        // made sense; a toppling column does the same job and belongs here.
        public static GameObject Column()
        {
            var root = new GameObject("Column");

            var stone = new Color(0.68f, 0.64f, 0.56f);
            var stoneDark = new Color(0.55f, 0.52f, 0.46f);
            var stoneLight = new Color(0.78f, 0.74f, 0.66f);

            // A stepped base, a fluted shaft in drums, and a capital: the
            // drums are what make it read as built rather than extruded.
            AddPart(root.transform, "Plinth", PrimitiveType.Cube, new Vector3(0f, 0.11f, 0f), new Vector3(1.12f, 0.22f, 1.12f), stoneDark);
            AddPart(root.transform, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.30f, 0f), new Vector3(0.94f, 0.10f, 0.94f), stone);

            // Four drums, not five. At full height a column stood nearly
            // three times the gorilla and a handful of them walled the
            // playfield in — cover should break up the space, not hide it.
            const int drums = 4;
            for (int i = 0; i < drums; i++)
            {
                float y = 0.46f + i * 0.60f;
                // A gentle taper toward the top, as a real column has.
                float w = Mathf.Lerp(0.78f, 0.64f, i / (float)(drums - 1));
                AddPart(root.transform, "Drum" + i, PrimitiveType.Cylinder, new Vector3(0f, y, 0f), new Vector3(w, 0.30f, w), i % 2 == 0 ? stone : stoneLight);
                // Thin joint line between drums.
                AddPart(root.transform, "Joint" + i, PrimitiveType.Cylinder, new Vector3(0f, y + 0.30f, 0f), new Vector3(w * 1.04f, 0.02f, w * 1.04f), stoneDark);
            }

            float top = 0.46f + (drums - 1) * 0.60f + 0.30f;
            AddPart(root.transform, "Capital", PrimitiveType.Cylinder, new Vector3(0f, top + 0.10f, 0f), new Vector3(0.88f, 0.10f, 0.88f), stone);
            AddPart(root.transform, "Abacus", PrimitiveType.Cube, new Vector3(0f, top + 0.26f, 0f), new Vector3(1.0f, 0.22f, 1.0f), stoneLight);

            // Same as Tree(): the attacks find fellable props by collider, so
            // a column without one can never be hit or knocked over.
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1.1f, 0f);
            col.radius = 0.42f;
            col.height = 2.2f;
            return root;
        }

        // What a toppled column leaves behind: a snapped stub on its plinth
        // with a couple of drums rolled off it.
        public static GameObject ColumnRubble()
        {
            var root = new GameObject("ColumnRubble");

            var stone = new Color(0.68f, 0.64f, 0.56f);
            var stoneDark = new Color(0.55f, 0.52f, 0.46f);

            AddPart(root.transform, "Plinth", PrimitiveType.Cube, new Vector3(0f, 0.11f, 0f), new Vector3(1.12f, 0.22f, 1.12f), stoneDark);
            AddPart(root.transform, "Stub", PrimitiveType.Cylinder, new Vector3(0f, 0.36f, 0f), new Vector3(0.80f, 0.22f, 0.80f), stone);

            for (int i = 0; i < 2; i++)
            {
                var chunk = AddPart(root.transform, "Chunk" + i, PrimitiveType.Cylinder,
                    new Vector3(Random.Range(-0.9f, 0.9f), 0.16f, Random.Range(-0.9f, 0.9f)),
                    new Vector3(0.62f, 0.22f, 0.62f), i % 2 == 0 ? stone : stoneDark);
                chunk.transform.localRotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
            }

            return root;
        }

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

        // A crescent laid on the ground tracing the path of a swing, built
        // from short blocks stepped along the arc. This is the shape the hit
        // test actually uses (MeleeArc), so the player can see where the
        // swipe reaches instead of inferring it from an expanding circle
        // that never matched.
        public static GameObject SwipeArc(Color color, float radius, float arcDegrees, float bandWidth)
        {
            // A REAL arc: one generated ring-sector mesh, not a row of boxes
            // laid along a curve. The boxes left visible corners and gaps
            // between them, tapered to a point at the tips, and so drew a
            // shape the hit test does not use — the test is an even band at
            // a fixed distance, and this is now exactly that band.
            var root = new GameObject("SwipeArc");

            const int steps = 28;
            float half = arcDegrees * 0.5f;
            float inner = Mathf.Max(0.01f, radius - bandWidth);
            float outer = radius + bandWidth;

            var vertices = new Vector3[(steps + 1) * 2];
            var triangles = new int[steps * 6];

            for (int i = 0; i <= steps; i++)
            {
                float angle = Mathf.Lerp(-half, half, i / (float)steps);
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                vertices[i * 2] = dir * inner;
                vertices[i * 2 + 1] = dir * outer;
            }

            for (int i = 0; i < steps; i++)
            {
                int v = i * 2;
                int t = i * 6;
                triangles[t] = v;
                triangles[t + 1] = v + 1;
                triangles[t + 2] = v + 3;
                triangles[t + 3] = v;
                triangles[t + 4] = v + 3;
                triangles[t + 5] = v + 2;
            }

            var mesh = new Mesh { name = "SwipeArcMesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var filter = root.AddComponent<MeshFilter>();
            filter.mesh = mesh;

            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = MaterialCache.GetUnlit(color);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

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
