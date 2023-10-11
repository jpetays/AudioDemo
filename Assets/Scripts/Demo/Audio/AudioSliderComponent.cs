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
            OnSliderValueChanged(_slider.value);
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
            OnSliderValueChanged(_slider.value);
        }

        private void OnSliderValueChanged(float sliderValue)
        {
            _sliderValue = _slider.value;
            _volumeDbValue = _audioChannel.UpdateChannel(_slider.normalizedValue, _isMuted);
            _sliderText.text =
                $"{_sliderTitle}: {sliderValue:0} ({_slider.normalizedValue:0.00}) ~ {_volumeDbValue:0.00} dB";
            Debug.Log(_sliderText.text);
        }
    }
}
