using System;
using UnityEngine;
using UnityEngine.UI;

namespace Prg.Window
{
    [RequireComponent(typeof(Button))]
    public class HtmlUrlButton : MonoBehaviour
    {
        [Header("Settings"), SerializeField] private string _urlToLoad;

        private void Awake()
        {
            var button = GetComponent<Button>();
            button.onClick.AddListener(OpenURL);
        }

        private void OpenURL()
        {
            var canCreate = Uri.TryCreate(_urlToLoad, UriKind.Absolute, out var uri);
            if (!canCreate)
            {
                throw new UnityException($"can not create URL: {_urlToLoad}");
            }
            if (uri.Scheme != "https")
            {
                throw new UnityException($"unsafe URL: {_urlToLoad}");
            }
            Debug.Log(uri.AbsoluteUri);
            Application.OpenURL(uri.AbsoluteUri);
        }
    }
}
