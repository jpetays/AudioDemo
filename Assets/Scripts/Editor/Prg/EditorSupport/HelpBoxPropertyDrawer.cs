using System.Text.RegularExpressions;
using Prg.EditorSupport;
using UnityEditor;
using UnityEngine;

namespace Editor.Prg.EditorSupport
{
    [CustomPropertyDrawer(typeof(HelpBoxAttribute))]
    public class HelpBoxPropertyDrawer : PropertyDrawer
    {
        private static readonly Regex RegExpLines = new(@"$", RegexOptions.Compiled | RegexOptions.Multiline);

        // HelpBox style has smaller font size, this seems to work well for few lines without too much extra space.
        private static readonly float HelpLineHeight = EditorGUIUtility.singleLineHeight * 0.667f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (attribute is not HelpBoxAttribute helpBox)
            {
                return;
            }
            EditorGUI.HelpBox(position, helpBox.text, MessageType.None);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (attribute is not HelpBoxAttribute helpBox)
            {
                return base.GetPropertyHeight(property, null);
            }
            var lineCount = RegExpLines.Matches(helpBox.text).Count;
            if (lineCount == 1)
            {
                return EditorGUIUtility.singleLineHeight;
            }
            return EditorGUIUtility.singleLineHeight + (lineCount - 1) * HelpLineHeight;
        }
    }
}
