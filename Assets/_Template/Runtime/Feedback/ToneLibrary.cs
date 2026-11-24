using System.Collections.Generic;
using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// Generates simple UI sounds at runtime so the template needs no audio files.
    /// Replace with your own AudioClips whenever you like (InteractableFeedback has clip slots).
    /// </summary>
    public static class ToneLibrary
    {
        private static readonly Dictionary<int, AudioClip> cache = new Dictionary<int, AudioClip>();

        /// <summary>A short sine "blip" with a fast decay.</summary>
        public static AudioClip Blip(float frequency, float duration = 0.08f)
        {
            int key = Mathf.RoundToInt(frequency) * 10000 + Mathf.RoundToInt(duration * 1000f);
            if (cache.TryGetValue(key, out var clip) && clip != null) return clip;

            const int sampleRate = 44100;
            int samples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float env = Mathf.Exp(-t * 40f / duration * 0.1f) * Mathf.Clamp01(i / 64f);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * env * 0.5f;
            }
            clip = AudioClip.Create($"Blip_{frequency}", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            cache[key] = clip;
            return clip;
        }

        public static void Play(AudioClip clip, Vector3 position, float volume = 0.6f)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }
    }
}
