using UnityEngine;

namespace GorillaSurvivors.Environment
{
    // A hard deadline on a temporary object.
    //
    // Every transient effect in the game cleans itself up at the end of the
    // coroutine that animates it, which is correct right up until something
    // stops that coroutine — a destroyed object it was following, a disabled
    // GameObject, an exception on a frame nobody was watching. Then the
    // effect is simply there, on the ground, for the rest of the run.
    //
    // This is the backstop for that whole class of bug: a marker cannot
    // outlive its deadline no matter what happens to the code that made it.
    // Deliberately on UNSCALED time, so a pause doesn't freeze the timer the
    // same way it freezes the coroutines this exists to catch.
    public class TimedDespawn : MonoBehaviour
    {
        public float Seconds = 6f;

        float _dieAt;

        public static void After(GameObject go, float seconds)
        {
            if (go == null) return;
            // Not `??`: that operator bypasses Unity's overloaded equality,
            // so a missing component comes back as a non-null reference.
            var timer = go.GetComponent<TimedDespawn>();
            if (timer == null) timer = go.AddComponent<TimedDespawn>();
            timer.Seconds = seconds;
            timer._dieAt = Time.unscaledTime + seconds;
        }

        void OnEnable()
        {
            if (_dieAt <= 0f) _dieAt = Time.unscaledTime + Seconds;
        }

        void Update()
        {
            if (Time.unscaledTime >= _dieAt) Destroy(gameObject);
        }
    }
}
