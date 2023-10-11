using UnityEngine;
using UnityEngine.Assertions;

namespace Demo.Audio
{
    public class AudioSettingsController : MonoBehaviour
    {
        [SerializeField, Header("Settings")] private AudioSettingsView _view;

        private void Awake()
        {
            Assert.IsNotNull(_view);
        }

        private void OnEnable()
        {
            _view.RemoveAllListeners();
            _view.ResetView();
        }
    }
}
