using System.Collections;
using Prg.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Demo
{
    [RequireComponent(typeof(AudioSource), typeof(Button))]
    public class PlayAudio : MonoBehaviour
    {
        [SerializeField, Header("Live Data")] private Button _button;
        [SerializeField] private AudioSource[] _audioSources;
        [SerializeField] private float _maxPlayTime;

        private YieldInstruction _waitForPlay;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _audioSources = gameObject.FindAudioSources();
            _maxPlayTime = _audioSources.GetMaxPlayTime();
            _waitForPlay = new WaitForSeconds(_maxPlayTime);
            _button.onClick.AddListener(OnPlay);
        }

        private void OnPlay()
        {
            StartCoroutine(PlayEffects());
        }

        private IEnumerator PlayEffects()
        {
            _button.interactable = false;
            _audioSources.Play();
            yield return _waitForPlay;
            _button.interactable = true;
        }
    }
}
