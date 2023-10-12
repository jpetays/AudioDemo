using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Audio;

namespace Prg.Util
{
    public static class AudioSourceUtil
    {
        public static AudioSource[] FindAudioSources(this GameObject gameObject)
        {
            return gameObject.GetComponentsInChildren<AudioSource>(true);
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
                audioSource.playOnAwake = true;
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
                audioSource.Play();
            }
        }
    }
}
