using UnityEngine;

namespace GorillaSurvivors.Core
{
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
            AddPrimitive(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0, 1.15f, 0.05f), Vector3.one * 0.55f, fur);
            AddPrimitive(root.transform, "EarL", PrimitiveType.Sphere, new Vector3(-0.28f, 1.32f, 0f), Vector3.one * 0.16f, furDark);
            AddPrimitive(root.transform, "EarR", PrimitiveType.Sphere, new Vector3(0.28f, 1.32f, 0f), Vector3.one * 0.16f, furDark);
            AddPrimitive(root.transform, "Face", PrimitiveType.Sphere, new Vector3(0, 1.05f, 0.28f), new Vector3(0.32f, 0.28f, 0.2f), skin);
            AddPrimitive(root.transform, "EyeL", PrimitiveType.Sphere, new Vector3(-0.1f, 1.12f, 0.38f), Vector3.one * 0.06f, Color.black);
            AddPrimitive(root.transform, "EyeR", PrimitiveType.Sphere, new Vector3(0.1f, 1.12f, 0.38f), Vector3.one * 0.06f, Color.black);
            AddCapsule(root.transform, "ArmL", new Vector3(-0.62f, 0.55f, 0f), 0.15f, 0.55f, fur);
            AddCapsule(root.transform, "ArmR", new Vector3(0.62f, 0.55f, 0f), 0.15f, 0.55f, fur);

            return root;
        }

        public static GameObject Human()
        {
            var root = new GameObject("HumanModel");

            var skin = new Color(0.85f, 0.68f, 0.58f);
            var shirt = new Color(0.85f, 0.22f, 0.20f);
            var pants = new Color(0.30f, 0.30f, 0.36f);
            var hair = new Color(0.20f, 0.15f, 0.12f);

            AddCapsule(root.transform, "LegL", new Vector3(-0.13f, 0.32f, 0f), 0.11f, 0.6f, pants);
            AddCapsule(root.transform, "LegR", new Vector3(0.13f, 0.32f, 0f), 0.11f, 0.6f, pants);
            AddCapsule(root.transform, "ArmL", new Vector3(-0.32f, 0.85f, 0f), 0.09f, 0.55f, skin);
            AddCapsule(root.transform, "ArmR", new Vector3(0.32f, 0.85f, 0f), 0.09f, 0.55f, skin);
            AddPrimitive(root.transform, "Torso", PrimitiveType.Capsule, new Vector3(0, 0.85f, 0), new Vector3(0.5f, 0.4f, 0.32f), shirt);
            AddPrimitive(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0, 1.42f, 0), Vector3.one * 0.34f, skin);
            AddPrimitive(root.transform, "Hair", PrimitiveType.Sphere, new Vector3(0, 1.52f, -0.02f), new Vector3(0.36f, 0.24f, 0.36f), hair);

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
            var tip = new Color(0.45f, 0.35f, 0.12f);

            var body = AddCapsule(root.transform, "Body", new Vector3(0, 0.35f, 0), 0.12f, 0.55f, yellow);
            body.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
            AddPrimitive(root.transform, "TipA", PrimitiveType.Sphere, new Vector3(-0.2f, 0.6f, 0f), Vector3.one * 0.08f, tip);
            AddPrimitive(root.transform, "TipB", PrimitiveType.Sphere, new Vector3(0.2f, 0.1f, 0f), Vector3.one * 0.07f, tip);

            return root;
        }

        public static GameObject Adrenaline()
        {
            var root = new GameObject("AdrenalineModel");
            var cyan = new Color(0.25f, 0.85f, 0.95f);
            var shard = AddPrimitive(root.transform, "Shard", PrimitiveType.Cube, new Vector3(0, 0.35f, 0), new Vector3(0.22f, 0.4f, 0.22f), cyan);
            shard.transform.localRotation = Quaternion.Euler(0f, 45f, 45f);
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
    }
}
