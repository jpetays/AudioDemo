using System.Collections;
using Prg.Util;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Demo
{
    /// <summary>
    /// Plays given <c>AudioSource</c>s on button click.
    /// </summary>
    /// <remarks>
    /// Button is disabled during play, unless it is <i>interruptable</i> and has exactly one <c>AudioSource</c>.
    /// </remarks>
    [RequireComponent(typeof(AudioSource), typeof(Button))]
    public class PlayAudioButton : MonoBehaviour
    {
        private const string Tp1 =
            "Audio effects can be played once without disabling button (if there is exactly one audio source";

        [SerializeField, Tooltip(Tp1), Header("Live Data")] private bool _isInterruptable;

        [SerializeField, Header("Live Data")] private Button _button;
        [SerializeField] private AudioSource[] _audioSources;
        [SerializeField] private bool _canInterrupt;
        [SerializeField] private float _maxPlayTime;

        private YieldInstruction _waitForPlay;

        private void Awake()
        {
            _button = GetComponent<Button>();
            Assert.IsNotNull(_button, "button component is required");
            _audioSources = gameObject.FindAudioSources();
            Assert.IsTrue(_audioSources.Length > 0, "must have at least one audio source");
            _canInterrupt = _isInterruptable && _audioSources.Length == 1;
            _maxPlayTime = _audioSources.GetMaxPlayTime();
            _button.onClick.AddListener(_canInterrupt
                ? () => PlayOnce(_audioSources[0])
                : () => StartCoroutine(PlayAudio()));
        }

        private static void PlayOnce(AudioSource audioSource)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            audioSource.Play();
        }

        private IEnumerator PlayAudio()
        {
            _waitForPlay ??= new WaitForSeconds(_maxPlayTime);
            _button.interactable = false;
            _audioSources.Play();
            yield return _waitForPlay;
            _button.interactable = true;
        }
    }
}
