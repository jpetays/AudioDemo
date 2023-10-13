using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace Demo.Audio
{
    /// <summary>
    /// <c>AudioSettingsView</c> for game audio settings UI.
    /// </summary>
    public class AudioSettingsView : MonoBehaviour
    {
        [SerializeField, Header("Settings")] private TextMeshProUGUI _windowTitle;
        [SerializeField] private TextMeshProUGUI _audioNotification;
        [SerializeField] private AudioSliderComponent _masterSlider;
        [SerializeField] private AudioSliderComponent _gameEffectsSlider;
        [SerializeField] private AudioSliderComponent _uiEffectsSlider;
        [SerializeField] private AudioSliderComponent _musicSlider;

        public string WindowTitle
        {
            set => _windowTitle.text = value;
        }

        public string AudioNotification
        {
            set => _audioNotification.text = value;
        }

        public bool IsDeviceMuted
        {
            set => AudioNotification = value ? "Device is muted" : "Volume is ON";
        }

        private void Awake()
        {
            Assert.IsNotNull(_masterSlider);
            Assert.IsNotNull(_gameEffectsSlider);
            Assert.IsNotNull(_uiEffectsSlider);
            Assert.IsNotNull(_musicSlider);
        }

        public void ResetView()
        {
            AudioNotification = $"Hello on {AppPlatform.Name}";
            _masterSlider.ResetComponent();
            _gameEffectsSlider.ResetComponent();
            _uiEffectsSlider.ResetComponent();
            _musicSlider.ResetComponent();
        }
    }
}
