using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Audio;

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
        public static AudioSettings Get() => Resources.Load<AudioSettings>(nameof(AudioSettings));

        public List<AudioChannelSetting> Settings;
    }

    /// <summary>
    /// <c>AudioSetting</c> for single audio channel that is played trough <c>AudioMixer</c>
    /// using given <c>AudioMixerGroup</c> during the game.
    /// </summary>
    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class AudioChannelSetting
    {
        [Header("Settings")] public AudioMixerGroup AudioMixerGroup;
        public VolumeNames _exposedVolumeName;

        public string ExposedVolumeName => _exposedVolumeName.ToString();
    }
}
