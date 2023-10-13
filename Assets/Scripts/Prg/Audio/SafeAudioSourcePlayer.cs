using Prg.EditorSupport;
using UnityEngine;
using UnityEngine.Assertions;

namespace Prg.Audio
{
    /// <summary>
    /// Optional WebGL 'safe' <c>AudioSource</c> player for 'Play On Awake' and ''Loop' kind of audio.
    /// </summary>
    /// <remarks>
    /// Because UNITY Audio Mixer ('Output') is not supported on WebGL platform
    /// we must disable it to avoid unnecessary error messages on console log.
    /// </remarks>
    [RequireComponent(typeof(AudioSource))]
    public class SafeAudioSourcePlayer : MonoBehaviour
    {
        private const string Notes = "WebGL 'safe' AudioSource wrapper.\r\n" +
                                     "if playing on WebGL this script will disable:\r\n" +
                                     "- 'Play On Awake' and 'Output' (AudioMixerGroup) on Awake()\r\n" +
                                     " - start playing audio OnEnable.\r\n" +
                                     "On other platforsm it does nothing.";

        // ReSharper disable once NotAccessedField.Local
        [SerializeField, Header(Notes), InspectorReadOnly] private string _;

        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            Assert.IsNotNull(_audioSource, "AudioSource is required");
            Assert.IsTrue(_audioSource.playOnAwake, "'Play On Awake' should be set");
            Assert.IsTrue(_audioSource.loop, "'Loop' should be set");
            Assert.IsNotNull(_audioSource.outputAudioMixerGroup, "'Output' (AudioMixerGroup) should be set");
            _audioSource.playOnAwake = false;
#if UNITY_WEBGL
            _audioSource.outputAudioMixerGroup = null;
#else
            enabled = false;
#endif
        }

        private void OnEnable()
        {
            _audioSource.Play();
        }
    }
}
