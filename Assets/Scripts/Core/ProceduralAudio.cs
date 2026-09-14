using UnityEngine;

namespace GorillaSurvivors.Core
{
    // Generates simple sound effects at runtime (sine tones, filtered noise,
    // and a tone+noise "impact" blend) — the audio counterpart to the
    // procedural art, so the game has SFX without needing any audio assets.
    public static class ProceduralAudio
    {
        const int SampleRate = 44100;

        // A sine tone with a linear decay envelope. `pitchSlide` shifts the
        // frequency linearly over the clip's duration (positive = rising,
        // negative = falling), which alone gives a lot of character (chimes,
        // dropping "hurt" bloops, rising "pickup" blips) without needing a
        // more complex synth.
        public static AudioClip Tone(float frequency, float duration, float volume = 0.5f, float pitchSlide = 0f)
        {
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float freq = Mathf.Max(20f, frequency + pitchSlide * t);
                float envelope = 1f - (i / (float)sampleCount);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * volume * envelope;
            }

            return BuildClip("Tone", data);
        }

        // Filtered white noise (a one-pole lowpass smooths harshness) with a
        // decay envelope — good for whooshes (dash), throws, and soft impacts.
        public static AudioClip Noise(float duration, float volume = 0.5f, float smoothing = 0.4f)
        {
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[sampleCount];
            float prev = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float envelope = 1f - (i / (float)sampleCount);
                float raw = Random.value * 2f - 1f;
                prev = Mathf.Lerp(prev, raw, smoothing);
                data[i] = prev * volume * envelope;
            }

            return BuildClip("Noise", data);
        }

        // A falling low tone blended with a burst of noise — reads as a
        // percussive "thud", good for the ground-slam / heavy hits.
        public static AudioClip Impact(float duration = 0.2f, float volume = 0.6f)
        {
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[sampleCount];
            float prevNoise = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float p = i / (float)sampleCount;
                float envelope = Mathf.Pow(1f - p, 2f);
                float tone = Mathf.Sin(2f * Mathf.PI * (110f - 60f * t) * t);
                float raw = Random.value * 2f - 1f;
                prevNoise = Mathf.Lerp(prevNoise, raw, 0.5f);
                data[i] = (tone * 0.7f + prevNoise * 0.5f) * volume * envelope;
            }

            return BuildClip("Impact", data);
        }

        static AudioClip BuildClip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
