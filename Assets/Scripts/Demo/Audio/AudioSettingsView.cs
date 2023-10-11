using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace Demo.Audio
{
    public class AudioSettingsView : MonoBehaviour
    {
        [SerializeField, Header("Settings")] private TextMeshProUGUI _audioNotification;
        [SerializeField] private AudioSliderComponent _masterSlider;

        public string AudioNotification
        {
            set => _audioNotification.text = value;
        }

        public AudioSliderComponent MasterSlider => _masterSlider;

        private void Awake()
        {
            Assert.IsNotNull(_masterSlider);
        }

        public void ResetView()
        {
            _masterSlider.ResetComponent();
            AudioNotification = "Hello"; //string.Empty;
        }
    }
}
