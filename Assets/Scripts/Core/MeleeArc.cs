using UnityEngine;

namespace GorillaSurvivors.Core
{
    // Hit test for the gorilla's melee attacks.
    //
    // These used to be spheres centred a fixed distance in front of the
    // player, which is easy but wrong in both directions at once: it reaches
    // nothing standing right against you (the sphere has already passed over
    // them) while happily hitting things off to the side and slightly behind
    // the centre point. A swing of an arm is a wedge — everything from here
    // out to arm's length, within some angle of where you're facing — so
    // that's what this tests.
    //
    // The wedge shape is also what makes "wider arc" a meaningful upgrade.
    public static class MeleeArc
    {
        // Fills `buffer` with colliders inside the wedge and returns how many.
        // Anything whose CENTRE is outside the wedge but whose body overlaps
        // it still counts, via a small radius allowance — otherwise a big
        // enemy standing right on the edge of the swing is missed in a way
        // the player can't see or predict.
        public static int Overlap(Vector3 origin, Vector3 aim, float reach, float arcDegrees, Collider[] buffer)
        {
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.0001f) aim = Vector3.forward;
            aim.Normalize();

            int found = Physics.OverlapSphereNonAlloc(origin, reach, buffer);
            float halfAngle = arcDegrees * 0.5f;

            int kept = 0;
            for (int i = 0; i < found; i++)
            {
                var col = buffer[i];
                if (col == null) continue;

                Vector3 toTarget = col.bounds.center - origin;
                toTarget.y = 0f;
                float distance = toTarget.magnitude;

                // Anything overlapping the player is inside the swing no
                // matter which way it is: you cannot be behind someone you
                // are standing inside of.
                if (distance > 0.35f)
                {
                    // How many degrees of slack this body's own width buys it
                    // at the distance it's standing.
                    float bodyRadius = Mathf.Max(col.bounds.extents.x, col.bounds.extents.z);
                    float slack = Mathf.Atan2(bodyRadius, distance) * Mathf.Rad2Deg;

                    if (Vector3.Angle(aim, toTarget / distance) > halfAngle + slack) continue;
                }

                buffer[kept++] = col;
            }

            return kept;
        }
    }
}
