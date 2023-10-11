using Prg;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Audio;
using UnityEngine.UI;
using Debug = Prg.Debug;

namespace Demo.Audio
{
    public class AudioSliderComponent : MonoBehaviour
    {
        private const float SliderMinValue = 0;
        private const float SliderMaxValue = 100f;

        private const float MixerMaxValue = 0;
        private const float MixerMinValue = -80f;

        [SerializeField, Header("Settings")] private VolumeNames _exposedVolume;
        [SerializeField] private string _sliderTitle;
        [SerializeField] private TextMeshProUGUI _sliderText;
        [SerializeField] private Slider _slider;
        [SerializeField] private bool _isWholeNumbers;
        [SerializeField] private Button _muteButton;

        [SerializeField, Header("Live Data")] private float _sliderValue;
        [SerializeField] private float _volumeDbValue;
        [SerializeField] private bool _isMuted;
        [SerializeField] private string _exposedVolumeName;
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private AudioChannelSetting _audioChannel;

        private void Awake()
        {
            _muteButton.onClick.AddListener(OnMuteButtonClicked);
            _slider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        private void OnEnable()
        {
            Debug.Log($"{_exposedVolume}", this);
            var audioSettings = AudioSettings.Get();
            _audioChannel = audioSettings.GetAudioChannelSettingBy(_exposedVolume);
            Assert.IsNotNull(_audioChannel, $"AudioChannelSetting not found for: {_exposedVolume}");
            Assert.IsNotNull(_audioChannel.AudioMixerGroup, $"AudioMixerGroup not found for: {_exposedVolume}");
            _audioMixer = _audioChannel.AudioMixerGroup.audioMixer;
            Assert.IsNotNull(_audioMixer, $"AudioMixer not found for: {_exposedVolume}");
            _exposedVolumeName = _audioChannel.ExposedVolumeName;
            if (string.IsNullOrWhiteSpace(_sliderTitle))
            {
                _sliderTitle = _exposedVolumeName;
            }

            // Show component state.
            _audioChannel.LoadState(out _sliderValue, out _isMuted);
            UpdateMuteButtonCaption();
            _slider.value = _sliderValue;
            // Must update slide manually for UI.
            OnSliderValueChanged(_sliderValue);
        }

        private void OnDisable()
        {
            // Save component state.
            _audioChannel.SaveState(_sliderValue, _isMuted);
        }

        public void ResetComponent()
        {
            _slider.minValue = SliderMinValue;
            _slider.maxValue = SliderMaxValue;
            _slider.wholeNumbers = _isWholeNumbers;
        }

        private void UpdateMuteButtonCaption()
        {
            _muteButton.SetCaption(_isMuted ? "Activate" : "Mute");
        }

        private void OnMuteButtonClicked()
        {
            _isMuted = !_isMuted;
            UpdateMuteButtonCaption();
            OnSliderValueChanged(_sliderValue);
        }

        private void OnSliderValueChanged(float sliderValue)
        {
            _volumeDbValue = SliderToDecibelUnity(_slider.normalizedValue);
            _sliderText.text =
                $"{_sliderTitle}: {sliderValue:0} ({_slider.normalizedValue:0.00} ~ {_volumeDbValue:0.00} dB)";
            if (_isMuted)
            {
                if (AudioMixerGetFloat() > MixerMinValue)
                {
                    AudioMixerSetFloat(MixerMinValue);
                }
                return;
            }
            AudioMixerSetFloat(_volumeDbValue);
        }

        private void AudioMixerSetFloat(float mixerValueDb)
        {
            if (mixerValueDb is < MixerMinValue or > MixerMaxValue)
            {
                Debug.Log($"Volume for '{_exposedVolumeName}' is out of range: {mixerValueDb:0.0}");
                mixerValueDb = Mathf.Clamp(mixerValueDb, MixerMinValue, MixerMaxValue);
            }
            Debug.Log($"{_exposedVolume} <- {mixerValueDb} dB ({_sliderValue:0} ~ {_slider.normalizedValue:0.00})", this);
            if (_audioMixer.SetFloat(_exposedVolumeName, mixerValueDb))
            {
                return;
            }
            Debug.Log($"AudioMixer parameter {_exposedVolumeName} not found", _audioChannel.AudioMixerGroup);
            throw new UnityException($"AudioMixer parameter {_exposedVolumeName} not found");
        }

        private float AudioMixerGetFloat()
        {
            if (_audioMixer.GetFloat(_exposedVolumeName, out var mixerValue))
            {
                return mixerValue;
            }
            Debug.Log($"AudioMixer parameter {_exposedVolumeName} not found", _audioChannel.AudioMixerGroup);
            throw new UnityException($"AudioMixer parameter {_exposedVolumeName} not found");
        }

        public static float SliderToDecibelUnity(float normalizedValue)
        {
            if (normalizedValue > 0)
            {
                if (normalizedValue < 1f)
                {
                    // Mathf.Log10 returns values between -4.0 ... 0.0 and
                    // multiplying this by 20.0 we got a range of -80.0 ... 0.0 decibels
                    // that we want the slider to travel from min to max.
                    return Mathf.Log10(normalizedValue) * 20f;
                }
                return MixerMaxValue;
            }
            return MixerMinValue;
        }
    }
}
