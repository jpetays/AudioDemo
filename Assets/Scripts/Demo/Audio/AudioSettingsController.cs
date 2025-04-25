using System.Text;
using Prg.PubSub;
using UnityEngine;
using UnityEngine.Assertions;

namespace Demo.Audio
{
    /// <summary>
    /// <c>AudioSettingsController</c> to manage game audio settings UI.
    /// </summary>
    public class AudioSettingsController : MonoBehaviour
    {
        [SerializeField, Header("Settings")] private AudioSettingsView _view;

        private void Awake()
        {
            Assert.IsNotNull(_view);
        }

        private void OnEnable()
        {
            _view.ResetView();
            var startupMessage = new StringBuilder()
                .Append(" Ver ").Append(Application.version)
                .Append(" Screen ").Append(AppPlatform.ScreeInfo())
                .ToString();
            _view.WindowTitle = $"Audio Settings\r\n{startupMessage}";
            if (!AppPlatform.IsMobile)
            {
                return;
            }
            _view.IsDeviceMuted = MobileAudio.MuteState == MobileAudio.MobileMuteState.IsMuted;
            this.Subscribe<AudioNotification>(OnAudioNotification);
        }

        private void OnDisable()
        {
            this.Unsubscribe();
        }

        private void OnAudioNotification(AudioNotification data)
        {
            _view.IsDeviceMuted = data.IsDeviceMuted;
        }
    }
}
