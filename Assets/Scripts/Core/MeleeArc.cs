using UnityEngine;

namespace GorillaSurvivors.Core
{
    // Hit test for the gorilla's melee attacks.
    //
    // The shape is the SWING ITSELF: a circular arc drawn in front of the
    // gorilla at arm's length, catching everything within some distance of
    // that arc. An arm sweeps through a band of space at a roughly fixed
    // radius from the shoulder, and that band is what this tests.
    //
    // Two earlier shapes were wrong in ways the player could see. A sphere
    // centred ahead of the gorilla reached nothing pressed against its chest
    // (the sphere had already passed over them) while hitting things off to
    // the side. A wedge from the origin fixed the first half but still swept
    // a solid pie slice, so a swipe connected with something standing on the
    // player's toes and something at full extension with equal authority,
    // and read as a cone of force rather than an arm.
    //
    // Both the arc's angular width and its radius are what the tech tree's
    // reach and "wider arc" nodes move.
    public static class MeleeArc
    {
        // Fills `buffer` with colliders touching the swing band and returns
        // how many.
        //
        //   arcRadius   distance from the player the arc is drawn at
        //   arcDegrees  total angular width of the arc, centred on `aim`
        //   bandWidth   how far either side of the arc line still counts
        //
        // A body's own width counts toward reaching the band, so a Brute
        // standing on the edge of a swing is hit — being missed by an attack
        // that visibly overlapped you is the one failure players notice.
        public static int Overlap(Vector3 origin, Vector3 aim, float arcRadius, float arcDegrees,
            float bandWidth, Collider[] buffer)
        {
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.0001f) aim = Vector3.forward;
            aim.Normalize();

            // Everything that could possibly touch the band is inside this.
            int found = Physics.OverlapSphereNonAlloc(origin, arcRadius + bandWidth + 1.5f, buffer);
            float halfAngle = arcDegrees * 0.5f;

            int kept = 0;
            for (int i = 0; i < found; i++)
            {
                var col = buffer[i];
                if (col == null) continue;

                Vector3 toTarget = col.bounds.center - origin;
                toTarget.y = 0f;
                float distance = toTarget.magnitude;

                float bodyRadius = Mathf.Max(col.bounds.extents.x, col.bounds.extents.z);
                if (DistanceToArc(origin, aim, halfAngle, arcRadius, col.bounds.center)
                    > bandWidth + bodyRadius) continue;

                buffer[kept++] = col;
            }

            return kept;
        }

        // Point test against the same band, for things that have no collider
        // to be found by an overlap — thrown rocks, mainly.
        public static bool Contains(Vector3 origin, Vector3 aim, float arcRadius, float arcDegrees,
            float bandWidth, Vector3 point, float pointRadius = 0f)
        {
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.0001f) aim = Vector3.forward;
            aim.Normalize();

            return DistanceToArc(origin, aim, arcDegrees * 0.5f, arcRadius, point)
                   <= bandWidth + pointRadius;
        }

        // How far a point lies from the swept arc line itself.
        static float DistanceToArc(Vector3 origin, Vector3 aim, float halfAngle, float arcRadius, Vector3 point)
        {
            Vector3 toTarget = point - origin;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            // Standing exactly on the player: the arc is arcRadius away.
            if (distance < 0.0001f) return arcRadius;

            float angle = Vector3.Angle(aim, toTarget / distance);
            // Inside the arc's sweep: only the radial gap matters.
            if (angle <= halfAngle) return Mathf.Abs(distance - arcRadius);

            // Past the end of the sweep: measure to the nearer tip of the
            // arc, so the swing stops where the hand does instead of at a
            // hard angular wall.
            float side = Vector3.Dot(Vector3.Cross(Vector3.up, aim), toTarget) >= 0f ? 1f : -1f;
            Vector3 tip = origin + (Quaternion.Euler(0f, side * halfAngle, 0f) * aim) * arcRadius;
            Vector3 toTip = point - tip;
            toTip.y = 0f;
            return toTip.magnitude;
        }
    }
}
