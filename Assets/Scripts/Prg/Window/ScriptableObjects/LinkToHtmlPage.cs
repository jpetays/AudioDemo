using UnityEngine;

namespace Prg.Window.ScriptableObjects
{
    [CreateAssetMenu(menuName = "Prg/LinkToHtmlPage", fileName = "link")]
    public class LinkToHtmlPage : ScriptableObject
    {
        [SerializeField] private string _theUrlToUse;
        [SerializeField] private string _theTextToShow;

        public string URL => _theUrlToUse;
        public string Text => _theTextToShow;
    }
}
