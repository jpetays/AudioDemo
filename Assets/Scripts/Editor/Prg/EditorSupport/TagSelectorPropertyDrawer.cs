// ------------------------------------------- //
// Author  : William Whitehouse / WSWhitehouse //
// GitHub  : github.com/WSWhitehouse           //
// Created : 30/06/2019                        //
// Edited  : 25/02/2020                        // 
// ------------------------------------------- //

using System.Collections.Generic;
using Prg.EditorSupport;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Editor.Prg.EditorSupport
{
    [CustomPropertyDrawer(typeof(TagSelectorAttribute))]
    public class TagSelectorPropertyDrawer : PropertyDrawer
    {
        private const string BadTagNameMarker = "<<<";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (attribute is not TagSelectorAttribute selectorAttribute ||
                property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }
            if (selectorAttribute.UseEditorGui)
            {
                EditorGUI.BeginProperty(position, label, property);
                {
                    property.stringValue = EditorGUI.TagField(position, label, property.stringValue);
                }
                EditorGUI.EndProperty();
                return;
            }
            EditorGUI.BeginProperty(position, label, property);
            {
                var tagList = new List<string>(InternalEditorUtility.tags);
                var displayList = new List<string>(tagList);
                var propertyString = property.stringValue;
                var index = tagList.FindIndex(x => x.Equals(propertyString));
                if (index == -1)
                {
                    var badProperty = $"{BadTagNameMarker} {propertyString} {BadTagNameMarker}";
                    displayList.Add(badProperty);
                    index = displayList.Count - 1;
                }
                var newIndex = EditorGUI.Popup(position, label.text, index, displayList.ToArray());
                if (newIndex != index && newIndex < tagList.Count)
                {
                    // Property was changed by user and is valid tag value.
                    property.stringValue = tagList[newIndex];
                }
            }
            EditorGUI.EndProperty();
        }
    }
}
