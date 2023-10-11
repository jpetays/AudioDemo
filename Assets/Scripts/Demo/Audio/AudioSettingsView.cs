using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace Demo.Audio
{
    public class AudioSettingsView : MonoBehaviour
    {
        [SerializeField, Header("Settings")] private TextMeshProUGUI _audioNotification;
        [SerializeField] private AudioSliderComponent _masterSlider;
        [SerializeField] private AudioSliderComponent _gameEffectsSlider;
        [SerializeField] private AudioSliderComponent _uiEffectsSlider;
        [SerializeField] private AudioSliderComponent _musicSlider;

        public string AudioNotification
        {
            set => _audioNotification.text = value;
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
            AudioNotification = "Hello"; //string.Empty;
            _masterSlider.ResetComponent();
            _gameEffectsSlider.ResetComponent();
            _uiEffectsSlider.ResetComponent();
            _musicSlider.ResetComponent();
        }
    }
}
