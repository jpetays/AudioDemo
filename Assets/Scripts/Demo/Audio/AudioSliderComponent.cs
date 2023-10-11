using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Demo.Audio
{
    public class AudioSliderComponent : MonoBehaviour
    {
        [SerializeField, Header("Settings")] private TextMeshProUGUI _sliderText;
        [SerializeField] private Slider _slider;
        [SerializeField] private bool _isWholeNumbers;
        [SerializeField] private Button _muteButton;
        [SerializeField] private AudioChannelSetting _audioChannel;

        public void RemoveAllListeners()
        {
            _slider.onValueChanged.RemoveAllListeners();
        }

        public void ResetComponent()
        {
            _slider.minValue = 0;
            _slider.maxValue = 100;
            _slider.wholeNumbers = _isWholeNumbers;
        }
    }
}
