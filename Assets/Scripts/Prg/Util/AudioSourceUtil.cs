using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Audio;

namespace Prg.Util
{
    /// <summary>
    /// Extension methods for UNITY audio.
    /// </summary>
    public static class AudioSourceUtil
    {
        public static AudioSource[] FindAudioSources(this GameObject gameObject, bool noPlayOnAwake = true)
        {
            var audioSources = gameObject.GetComponentsInChildren<AudioSource>(true);
#if UNITY_EDITOR
            if (!noPlayOnAwake)
            {
                return audioSources;
            }
            foreach (var audioSource in audioSources)
            {
                Assert.IsFalse(audioSource.playOnAwake,
                    $"audioSource.playOnAwake must be disabled on {audioSource.name}");
            }
#endif
            return audioSources;
        }

        public static AudioSource[] WithLoop(this AudioSource[] audioSources, bool loop)
        {
#if UNITY_EDITOR
            foreach (var audioSource in audioSources)
            {
                Assert.AreEqual(loop, audioSource.loop,
                    $"audioSource.loop must be set to {loop} on {audioSource.name}");
            }
#endif
            return audioSources;
        }

        public static float GetMaxPlayTime(this AudioSource[] audioSources, float curPlayDuration = 0)
        {
            if (audioSources.Length == 0)
            {
                return curPlayDuration;
            }
            var maxPlayDuration = curPlayDuration;
            foreach (var audioSource in audioSources)
            {
                if (audioSource.clip.length > maxPlayDuration)
                {
                    maxPlayDuration = audioSource.clip.length;
                }
            }
            return maxPlayDuration;
        }

        public static void SetPlayOnAwake(this AudioSource[] audioSources)
        {
            if (audioSources.Length == 0)
            {
                return;
            }
            foreach (var audioSource in audioSources)
            {
                Assert.IsNotNull(audioSource.outputAudioMixerGroup, $"{audioSource.name} must have AudioMixerGroup");
                audioSource.loop = false;
                audioSource.playOnAwake = true;
            }
        }

        public static void DisablePlayOnAwake(this AudioSource[] audioSources)
        {
            if (audioSources.Length == 0)
            {
                return;
            }
            foreach (var audioSource in audioSources)
            {
                Assert.IsNotNull(audioSource.outputAudioMixerGroup, $"{audioSource.name} must have AudioMixerGroup");
                audioSource.loop = false;
                audioSource.playOnAwake = false;
            }
        }

        public static void SetAudioMixerGroup(this AudioSource[] audioSources, AudioMixerGroup audioMixerGroup)
        {
            if (audioSources.Length == 0)
            {
                return;
            }
            foreach (var audioSource in audioSources)
            {
                audioSource.outputAudioMixerGroup = audioMixerGroup;
            }
        }

        public static void Play(this AudioSource[] audioSources)
        {
            if (audioSources.Length == 0)
            {
                return;
            }
            foreach (var audioSource in audioSources)
            {
                Assert.IsNotNull(audioSource.outputAudioMixerGroup, $"{audioSource.name} must have AudioMixerGroup");
                audioSource.enabled = true;
                audioSource.Play();
            }
        }

        public static void Stop(this AudioSource[] audioSources)
        {
            if (audioSources.Length == 0)
            {
                return;
            }
            foreach (var audioSource in audioSources)
            {
                audioSource.Stop();
            }
        }
    }
}
