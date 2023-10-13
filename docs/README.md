# AudioDemo

This is a simple UNITY demo how to use [AudioMixer](https://docs.unity3d.com/Manual/AudioMixer.html) to manage 'audio
channel' volume and mute state in a game.

_Unfortunately Audio Mixer does not work for [WebGL](https://docs.unity3d.com/Manual/webgl-audio.html) builds and they are just disabled so everything is played at 100% volume.  
UNITY has minimal support for audio in browsers as they have their own quite strict limitations._

Used UNITY version is 2022.3.4f1.

## Mobile Support

This demo has specific UNITY [mobile](https://docs.unity3d.com/ScriptReference/AudioSettings.Mobile.html) audio support
included.

Starting and stopping the _audio output thread_ on Android/iOS is managed by _MobileAudio_ class.  
Note that this uses included _Publish-Subscribe_ implementation to **broadcast** notifications when mobile device
audio 'mute state' changes -
if the game or its UI needs to be aware of these changes made by the user.

## Audio Channels

The demo has four audio channels identified by its **name** given in C# enum for convenience to use in UNITY Editor:

* MasterVolume
* GameEffectsVolume
* UiEffectsVolume
* MusicVolume

## AudioConfig (_ScriptableObject_)

AudioConfig has a list of audio channels that bind together audio channel **name** and
related [AudioMixerGroup](https://docs.unity3d.com/ScriptReference/Audio.AudioMixerGroup.html) that is used to control
audio for this channel.

AudioConfig is just a container and does not have any specific semantics over any audio channel how it is used.

### AudioChannelSetting (_Serializable_)

AudioChannelSetting is responsible for loading, saving and updating audio channel persistent settings and state.  
It manages _AudioMixerGroup_ associated with this audio channel.  
Audio channel persistent settings are saved in
UNITY [PlayerPrefs](https://docs.unity3d.com/ScriptReference/PlayerPrefs.html).

## User Interface

This demo has simple 'screens' for managing audio channels setup and testing them.

Game's _MusicVolume_ channel is started automatically when game is started to play included sample background music
forever.  
This is done using UNITY AudioSource with _Play On Awake_ and _Loop_ checked and using AudioMixedGroup for _MusicVolume_
channel without any code required for this.

### Audio Settings Screen

Audio Settings Screen has

* a notification area for mobile device 'mute state', and
* slider for each audio channel placed by UI designer.

_AudioSliderComponent_ is responsible for synchronizing UI slider state with related audio channel.  
Connection between slider and the audio channel (_AudioChannelSetting_) is done using audio channel **name**.

### Audio (Effect) Test Screen

Audio Test Screen has buttons to test Game and UI effects by pressing them.

### Testing

This demo has has been briefly tested on following platforms:

* Windows Desktop ☑
* Android ☑
* WebGL ⛔ ⛅

(⛅) _Music can be played but volume can not. Chaning volume for individual UNITY AudioSources requires different solution._
