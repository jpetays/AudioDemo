using UnityEngine;
using UnityEngine.UI;

namespace Prg.Window
{
    /// <summary>
    /// Exit Application button for those platforms where it makes sense.<br />
    /// Use <c>PlatformSelector</c> to select eligible platforms.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ExitApplicationButton : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(ExitApplication.ExitGracefully);
        }
    }
}
