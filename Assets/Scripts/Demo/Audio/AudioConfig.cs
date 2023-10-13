using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using Debug = Prg.Debug;

namespace Demo.Audio
{
    /// <summary>
    /// <c>AudioMixer</c> 'bindings' for the game.<br />
    /// This is just a list of available <c>AudioSettings</c>'
    /// so that we can manage given <c>AudioMixerGroup</c> at runtime byt its 'exposed parameter name'.
    /// </summary>
    /// <remarks>
    /// The class name is <b>not</b> <c>AudioSetting</c> because UNITY has a class with same name.<br />
    /// WebGL platform is <b>not</b> supported by UNITY with <c>AudioMixer</c>!
    /// </remarks>
    [CreateAssetMenu(menuName = "Prg/" + nameof(AudioConfig), fileName = nameof(AudioConfig))]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class AudioConfig : ScriptableObject
    {
        public const float SliderMaxValue = 100f;
        public const float SliderDefaultValue = 100f;

        public static AudioConfig Get() => Resources.Load<AudioConfig>(nameof(AudioConfig));

        public List<AudioChannelSetting> Settings;

        public AudioChannelSetting GetAudioChannelSettingBy(VolumeParamNames volumeParamName) =>
            Settings.Find(x => x._exposedVolumeParamName.Equals(volumeParamName));

        public static void Initialize()
        {
            var audioSettings = Get();
            if (AppPlatform.IsWebGL)
            {
                // We just remove all settings to prevent someone accidentally using them.
                audioSettings.Settings.Clear();
                return;
            }
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
    /// <remarks>
    /// We save volume (Audio Mixer attenuation) as a normalized value between 0.0 .. 1.0 (inclusive) and channel mute state.<br />
    /// Conversion to Audio Mixer decibel value is done using 'standard' UNITY formula: Mathf.Log10(<i>normalizedValue</i>) * 20f.
    /// </remarks>
    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class AudioChannelSetting
    {
        private const float MixerMaxValue = 0;
        private const float MixerMinValue = -80f;

        [Header("Settings")] public AudioMixerGroup AudioMixerGroup;
        [FormerlySerializedAs("_exposedVolumeName")] public VolumeParamNames _exposedVolumeParamName;

        public string ExposedVolumeName => _exposedVolumeParamName.ToString();

        private string PlayerPrefsName(string category, string name) => $"settings.audio.{category}.{name}";

        public void LoadState(out float sliderValue, out bool isMuted)
        {
            sliderValue = PlayerPrefs.GetFloat(PlayerPrefsName("volume", ExposedVolumeName),
                AudioConfig.SliderDefaultValue);
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
                Assert.IsFalse(normalizedValue > 1f,
                    $"normalizedValue is out of range (0-1): {normalizedValue:0.0}");
                return MixerMaxValue;
            }
            Assert.IsFalse(normalizedValue < 0,
                $"normalizedValue is out of range (0-1): {normalizedValue:0.0}");
            return MixerMinValue;
        }
    }
}
