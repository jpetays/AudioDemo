# Checklist for audio designers and implementors

## Part 1: Audio Designers

### Audio channels

Check UNITY [Audio Overview](https://docs.unity3d.com/Manual/AudioOverview.html) for general concepts etc.

Decide how many audio channels are required for the game and how they will be used in conjunction.  
Decide names for them add add those names to #C enum: **VolumeParamNames** in file **ExposedParameters.cs**.  
_(This needs to be done by Audio Programmers!)_  
These names are used to connect the game and its logic with actual UNITY Audio Mixer channels.

Names should
follow [PascalCase](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names) naming
convention.  
For example

* MasterVolume = 0,
* GameEffectsVolume = 1,
* UiEffectsVolume = 2,
* MusicVolume = 3

Note that all audio channels are equal (non hierarchical) from configuration perspective.
It is actual Audio Mixer configuration that defines relationships (if any) between different audio channels.

### UNITY Audio Mixer

We need at least one [Audio Mixer](https://docs.unity3d.com/Manual/AudioMixer.html) in order to create audio channels (
Audio Mixer Groups) for our purposes.

### UNITY Audio Mixer Group

Audio Mixer Group is the term and object used by UNITY to define what we call 'audio channel' for simplicity here.

Audio Mixer Group can 'expose' its properties for use by game program logic.
We use this feature to programmatically control audio channel volume.  
Actually we control Audio Mixer Group **Attenuation Unit** which has **Volume** (attenuation/gain) property we can
expose, give a name and thus change programmatically from our game.

#### Exposed parameters

Check chapter **Exposed parameters**
in [AudioGroup Inspector](https://docs.unity3d.com/Manual/AudioMixerInspectors.html) page to see how parameters can be '
exposed' in UNITY Editor.
This is not very intuitive for first time.  
_Note that Exposed parameters bypass the Snapshot system of an Audio Mixer and prevent applying any transitions to this
audio channel!_

### Audio Source

Audio Source is the thing that plays audio, and in our case it is used to set the audio channel for playing.

#### Output

We need to set **Output** for any of our **Audio Mixer Group** to direct this **Audio Source** to **Audio Mixer** for
playing trough it.

### Actual Configuration

Actual configuration so that game can find and access audio channels is done in **AudioConfig** (_ScriptableObject_)
that must be created in **Resources** folder with name **AudioConfig**.

This should contain one entry for each audio channel used by the game.  
Each entry has two pieces of required information:

* the **Audio Mixer Group** reference
* **Exposed Volume Name** (one name from VolumeParamNames setup earlier)

## Part 2: Audio Programmers

Programmatic access to manipulate audio channel volume requires that following three things are in place:

* UNITY **Audio Mixer**
* UNITY **Audio Mixer Group(s)**
* **AudioConfig** (_ScriptableObject_)

Rest is up to the programmers what they want to achieve or is required by them.

### AudioConfig.cs (_ScriptableObject_)

AudioConfig.cs contains the settings and is responsible of initializing audio channels when requested during game startup.

### AudioChannelSetting.cs

AudioChannelSetting.cs manages actual UNITY AudioMixerGroup (volume) and persistent settings related with it.

Following functions are used to save audio channel settings in
UNITY [PlayerPrefs](https://docs.unity3d.com/ScriptReference/PlayerPrefs.html).

```csharp
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
```

Following functions are used to update audio channel volume.

```csharp
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
```
