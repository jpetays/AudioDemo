using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Audio;
using Debug = Prg.Debug;

namespace Demo.Audio
{
    /// <summary>
    /// Names for allowed <c>AudioMixerGroup</c> exposed parameter names for attenuation (volume).
    /// </summary>
    /// <remarks>
    /// This is used to 'bind' Editor settings to runtime C# code.
    /// </remarks>
    public enum VolumeNames
    {
        MasterVolume = 0,
        GameEffectsVolume = 1,
        UiEffectsVolume = 2,
        MusicVolume = 3
    }

    /// <summary>
    /// <c>AudioMixer</c> settings for the game.
    /// </summary>
    /// <remarks>
    /// This is just a list of available <c>AudioSetting</c>s
    /// so that we can manage given <c>AudioMixerGroup</c> at runtime.
    /// </remarks>
    [CreateAssetMenu(menuName = "Prg/" + nameof(AudioSettings), fileName = nameof(AudioSettings))]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class AudioSettings : ScriptableObject
    {
        public const float SliderMaxValue = 100f;
        public const float SliderDefaultValue = 100f;

        public static AudioSettings Get() => Resources.Load<AudioSettings>(nameof(AudioSettings));

        public List<AudioChannelSetting> Settings;

        public AudioChannelSetting GetAudioChannelSettingBy(VolumeNames volumeName) =>
            Settings.Find(x => x._exposedVolumeName.Equals(volumeName));

        public static void Initialize()
        {
            var audioSettings = Get();
            foreach (var audioChannel in audioSettings.Settings)
            {
                audioChannel.LoadState(out var sliderValue, out var isMuted);
                var normalizedValue = LinearConversionInRange(0, SliderMaxValue, 0, 1f, sliderValue);
                var volumeDbValue = audioChannel.UpdateChannel(normalizedValue, isMuted);
                Debug.Log(
                    $"{audioChannel.ExposedVolumeName}: {sliderValue:0} ({normalizedValue:0.00}) ~ {volumeDbValue:0.00} dB isMuted {isMuted}");
            }
            return;

            float LinearConversionInRange(
                float originalStart, float originalEnd, // original range
                float newStart, float newEnd, // desired range
                float value) // value to convert
            {
                // https://stackoverflow.com/questions/4229662/convert-numbers-within-a-range-to-numbers-within-another-range
                var scale = (newEnd - newStart) / (originalEnd - originalStart);
                var result = newStart + (value - originalStart) * scale;
                return result;
            }
        }
    }

    /// <summary>
    /// <c>AudioSetting</c> for single audio channel that is played trough <c>AudioMixer</c>
    /// using given <c>AudioMixerGroup</c> during the game.
    /// </summary>
    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class AudioChannelSetting
    {
        private const float MixerMaxValue = 0;
        private const float MixerMinValue = -80f;

        [Header("Settings")] public AudioMixerGroup AudioMixerGroup;
        public VolumeNames _exposedVolumeName;

        public string ExposedVolumeName => _exposedVolumeName.ToString();

        private string PlayerPrefsName(string category, string name) => $"settings.audio.{category}.{name}";

        public void LoadState(out float sliderValue, out bool isMuted)
        {
            sliderValue = PlayerPrefs.GetFloat(PlayerPrefsName("volume", ExposedVolumeName),
                AudioSettings.SliderDefaultValue);
            isMuted = PlayerPrefs.GetInt(PlayerPrefsName("mute", ExposedVolumeName), 0) != 0;
        }

        public void SaveState(float sliderValue, bool isMuted)
        {
            PlayerPrefs.SetFloat(PlayerPrefsName("volume", ExposedVolumeName), sliderValue);
            PlayerPrefs.SetInt(PlayerPrefsName("mute", ExposedVolumeName), isMuted ? 1 : 0);
        }

        public float UpdateChannel(float normalizedValue, bool isMuted)
        {
            var decibelValue = ConvertToDecibelUnity(normalizedValue);
            if (isMuted)
            {
                if (AudioMixerGetFloat() > MixerMinValue)
                {
                    AudioMixerSetFloat(MixerMinValue);
                }
            }
            else
            {
                AudioMixerSetFloat(decibelValue);
            }
            return decibelValue;
        }

        private void AudioMixerSetFloat(float mixerValueDb)
        {
            if (mixerValueDb is < MixerMinValue or > MixerMaxValue)
            {
                Debug.Log($"Volume for '{ExposedVolumeName}' is out of range: {mixerValueDb:0.0}");
                mixerValueDb = Mathf.Clamp(mixerValueDb, MixerMinValue, MixerMaxValue);
            }
            if (AudioMixerGroup.audioMixer.SetFloat(ExposedVolumeName, mixerValueDb))
            {
                return;
            }
            Debug.Log($"AudioMixer parameter {ExposedVolumeName} not found", AudioMixerGroup.audioMixer);
            throw new UnityException($"AudioMixer parameter {ExposedVolumeName} not found");
        }

        private float AudioMixerGetFloat()
        {
            if (AudioMixerGroup.audioMixer.GetFloat(ExposedVolumeName, out var mixerValue))
            {
                return mixerValue;
            }
            Debug.Log($"AudioMixer parameter {ExposedVolumeName} not found", AudioMixerGroup.audioMixer);
            throw new UnityException($"AudioMixer parameter {ExposedVolumeName} not found");
        }

        /// <summary>
        /// Converts liner value to logarithmic decibel value.
        /// </summary>
        /// <remarks>
        /// This formula attenuates volume approximately -6 dB when slider travels to 50.<br />
        /// Generally approved volume attenuation is about -10 dB when sound is perceived to be half of its previous value.<br />
        /// <i>Better formula could have volume approximately -10 dB when slider travels to 50</i>.
        /// </remarks>
        /// <param name="normalizedValue"></param>
        /// <returns></returns>
        public static float ConvertToDecibelUnity(float normalizedValue)
        {
            if (normalizedValue > 0)
            {
                if (normalizedValue < 1f)
                {
                    // Mathf.Log10 returns values between -4.0 ... 0.0 (for 0.0001 .. 1.0) and
                    // multiplying this by 20.0 we got a range of -80.0 ... 0.0 decibels
                    // that we want the slider to travel from min to max.
                    return Mathf.Log10(normalizedValue) * 20f;
                }
                Assert.IsFalse(!(normalizedValue > MixerMaxValue),
                    $"normalizedValue is out of range: {normalizedValue:0.0}");
                return MixerMaxValue;
            }
            Assert.IsFalse(!(normalizedValue < MixerMaxValue),
                $"normalizedValue is out of range: {normalizedValue:0.0}");
            return MixerMinValue;
        }
    }
}
