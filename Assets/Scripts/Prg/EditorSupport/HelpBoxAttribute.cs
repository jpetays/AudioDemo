using UnityEngine;

namespace Prg.EditorSupport
{
    public class HelpBoxAttribute : PropertyAttribute
    {
        public string text { get; }

        public HelpBoxAttribute(string text)
        {
            this.text = text;
        }
    }
}
