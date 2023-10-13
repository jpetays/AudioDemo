namespace Demo.Audio
{
    /// <summary>
    /// Names for allowed <c>AudioMixerGroup</c> exposed parameter names for attenuation (volume).
    /// </summary>
    /// <remarks>
    /// This is used to 'bind' Editor settings to runtime C# code.
    /// </remarks>
    public enum VolumeParamNames
    {
        MasterVolume = 0,
        GameEffectsVolume = 1,
        UiEffectsVolume = 2,
        MusicVolume = 3
    }

}
