using System.Text.RegularExpressions;
using Prg.Anim;
using UnityEditor;
using UnityEngine;

namespace Editor.Prg.EditorSupport
{
    [CustomPropertyDrawer(typeof(AnimatorDef), true)]
    public class AnimatorDefPropertyDrawer : PropertyDrawer
    {
        private static readonly Regex RegExpLines = new(@"$", RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly float LineHeight1 = EditorGUIUtility.singleLineHeight
                                                     + 2f * EditorGUIUtility.standardVerticalSpacing;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);
            {
                var lineHeight2 = position.height - LineHeight1;

                position.height = LineHeight1;
                var animatorProp = property.FindPropertyRelative(AnimatorDef.AnimatorName);
                label.text = AnimatorDef.AnimatorName;
                EditorGUI.PropertyField(position, animatorProp, label);

                position.y += LineHeight1;
                position.height = lineHeight2;
                var stateNamesProp = property.FindPropertyRelative(AnimatorDef.StateNamesName);
                label.text = AnimatorDef.StateNamesName;
                EditorGUI.PropertyField(position, stateNamesProp, label);
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // This is multi-lines property
            var stateNamesProp = property.FindPropertyRelative(AnimatorDef.StateNamesName);
            var stateNames = stateNamesProp.stringValue;
            var stateLines = RegExpLines.Matches(stateNames).Count;
            return LineHeight1
                   + stateLines * EditorGUIUtility.singleLineHeight;
        }
    }
}
