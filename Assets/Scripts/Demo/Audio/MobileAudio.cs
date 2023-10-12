using Prg.PubSub;
using UnityEngine;
using Debug = Prg.Debug;

namespace Demo.Audio
{
    public record AudioNotification
    {
        public readonly bool IsDeviceMuted;

        public AudioNotification(bool isDeviceMuted)
        {
            IsDeviceMuted = isDeviceMuted;
        }
    }

    /// <summary>
    /// Helper for Mobile audio settings.<br />
    /// https://docs.unity3d.com/ScriptReference/AudioSettings.Mobile.html<br />
    /// There is also 'Mute Other Audio Sources' in the Player Settings that might be of interest.<br />
    /// https://docs.unity3d.com/ScriptReference/PlayerSettings-muteOtherAudioSources.html
    /// </summary>
    /// <remarks>
    /// This works when music synchronization (on mute/un-mute) is not important or managed elsewhere.<br />
    /// UNITY <c>AudioSource</c>s are frozen when audio is muted and un-frozen when un-muted.<br />
    /// <b>But</b> <c>AudioSource</c> <b>play state</b> is not changed and
    /// they will continue where they was left on un-mute!<br />
    /// <i>Note that this class has not been tested extensively [read: not at all device(s)].</i>
    /// </remarks>
    public static class MobileAudio
    {
        public enum MobileMuteState
        {
            Unknown = 0,
            IsMuted = 1,
            IsNotMuted = 2
        }

        public static MobileMuteState MuteState { get; private set; } = MobileMuteState.Unknown;

        private static readonly Object UnityPublisher = new ();

        public static void Initialize()
        {
            Debug.Log($"audioOutputStarted={AudioSettings.Mobile.audioOutputStarted}");
            AudioSettings.Mobile.stopAudioOutputOnMute = true;
            AudioSettings.Mobile.OnMuteStateChanged += UpdateMuteState;
            // Set initial state manually.
            UpdateMuteState(AudioSettings.Mobile.muteState);
        }

        private static void UpdateMuteState(bool isMuted)
        {
            Debug.Log(
                $"audioOutputStarted={AudioSettings.Mobile.audioOutputStarted} {MobileMuteState.IsMuted} <- {(isMuted ? "mute" : "un-mute")}");
            MuteState = isMuted ? MobileMuteState.IsMuted : MobileMuteState.IsNotMuted;
            if (isMuted)
            {
                AudioSettings.Mobile.StopAudioOutput();
            }
            else
            {
                AudioSettings.Mobile.StartAudioOutput();
            }
            var audioNotification = new AudioNotification(isMuted);
            UnityPublisher.Publish(audioNotification);
        }
    }
}
