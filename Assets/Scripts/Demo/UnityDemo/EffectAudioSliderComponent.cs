using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace Demo.UnityDemo
{
    /// <summary>
    /// <c>AudioSliderComponent</c> UI component for audio effects using audio sample to play when volume is changed.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class EffectAudioSliderComponent : AudioSliderComponent
    {
        [SerializeField, Header("Sample Effect")] private AudioSource _sampleEffect;
        [SerializeField, Min(0)] private float _playGracePeriod = 0.2f;

        private float _lastPlayTime;
        private Coroutine _delayedPlay;
        private YieldInstruction _delayedPlayWait;

        protected override void Awake()
        {
            base.Awake();
            Assert.IsNotNull(_sampleEffect, "_sampleEffect is mandatory");
            _delayedPlayWait = new WaitForSeconds(_playGracePeriod);
        }

        protected override void OnSliderStateChanged()
        {
            base.OnSliderStateChanged();
            if (!IsSliderReady)
            {
                return;
            }
            PlaySampleEffect();
        }

        private void PlaySampleEffect()
        {
            // Update time to keep on playing.
            _lastPlayTime = Time.time + _playGracePeriod;
            if (_delayedPlay != null)
            {
                return;
            }
            _delayedPlay = StartCoroutine(PlaySampleUntilDone());
        }

        private IEnumerator PlaySampleUntilDone()
        {
            var nextPlayTime = Time.time + _playGracePeriod;
            while (Time.time < _lastPlayTime)
            {
                yield return null;
                if (!(Time.time > nextPlayTime))
                {
                    continue;
                }
                // Play sample during slider change.
                if (_sampleEffect.isPlaying)
                {
                    _sampleEffect.Stop();
                }
                yield return null;
                _sampleEffect.Play();
                nextPlayTime = Time.time + _playGracePeriod;
            }
            // Mark that sample has ended playing.
            _delayedPlay = null;
            // Wait to see if slider has really stopped moving for long enough.
            yield return _delayedPlayWait;
            if (!(Time.time < _lastPlayTime))
            {
                yield break;
            }
            // Play full final sample after slider has really stopped moving.
            if (_sampleEffect.isPlaying)
            {
                _sampleEffect.Stop();
            }
            yield return null;
            _sampleEffect.Play();
        }
    }
}
