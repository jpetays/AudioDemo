using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Prg.EditorSupport;
using UnityEditor;
using UnityEngine;

namespace Editor.Prg.EditorSupport
{
    /// <summary>
    /// Property drawer for <c>InspectorReadOnly</c> attribute.
    /// </summary>
    [CustomPropertyDrawer(typeof(InspectorReadOnlyAttribute))]
    public class InspectorReadOnlyPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            var propValue = property.GetValue<object>();
            EditorGUI.LabelField(position, propValue?.ToString());
        }
    }

    /// <summary>
    /// https://gist.github.com/douduck08/6d3e323b538a741466de00c30aa4b61f
    /// </summary>
    public static class SerializedPropertyExtension
    {
        private static readonly Regex RegExp = new Regex(@"\[\d+\]", RegexOptions.Compiled);

        public static T GetValue<T>(this SerializedProperty property) where T : class
        {
            object obj = property.serializedObject.targetObject;
            var path = property.propertyPath.Replace(".Array.data", "");
            var fieldStructure = path.Split('.');
            for (var i = 0; i < fieldStructure.Length; i++)
            {
                if (fieldStructure[i].Contains("["))
                {
                    var index = Convert.ToInt32(new string(fieldStructure[i].Where(c => char.IsDigit(c))
                        .ToArray()));
                    obj = GetFieldValueWithIndex(RegExp.Replace(fieldStructure[i], ""), obj, index);
                }
                else
                {
                    obj = GetFieldValue(fieldStructure[i], obj);
                }
            }
            return (T)obj;
        }

        public static bool SetValue<T>(this SerializedProperty property, T value) where T : class
        {
            object obj = property.serializedObject.targetObject;
            var path = property.propertyPath.Replace(".Array.data", "");
            var fieldStructure = path.Split('.');
            for (var i = 0; i < fieldStructure.Length - 1; i++)
            {
                if (fieldStructure[i].Contains("["))
                {
                    var index = Convert.ToInt32(new string(fieldStructure[i].Where(c => char.IsDigit(c))
                        .ToArray()));
                    obj = GetFieldValueWithIndex(RegExp.Replace(fieldStructure[i], ""), obj, index);
                }
                else
                {
                    obj = GetFieldValue(fieldStructure[i], obj);
                }
            }

            var fieldName = fieldStructure.Last();
            if (fieldName.Contains("["))
            {
                var index = Convert.ToInt32(new string(fieldName.Where(c => char.IsDigit(c)).ToArray()));
                return SetFieldValueWithIndex(RegExp.Replace(fieldName, ""), obj, index, value);
            }
            else
            {
                return SetFieldValue(fieldName, obj, value);
            }
        }

        private static object GetFieldValue(string fieldName, object obj,
            BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                    BindingFlags.NonPublic)
        {
            var field = obj.GetType().GetField(fieldName, bindings);
            if (field != null)
            {
                return field.GetValue(obj);
            }
            return default(object);
        }

        private static object GetFieldValueWithIndex(string fieldName, object obj, int index,
            BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                    BindingFlags.NonPublic)
        {
            var field = obj.GetType().GetField(fieldName, bindings);
            if (field != null)
            {
                var list = field.GetValue(obj);
                if (list.GetType().IsArray)
                {
                    return ((object[])list)[index];
                }
                else if (list is IEnumerable)
                {
                    return ((IList)list)[index];
                }
            }
            return default(object);
        }

        public static bool SetFieldValue(string fieldName, object obj, object value, bool includeAllBases = false,
            BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                    BindingFlags.NonPublic)
        {
            var field = obj.GetType().GetField(fieldName, bindings);
            if (field != null)
            {
                field.SetValue(obj, value);
                return true;
            }
            return false;
        }

        public static bool SetFieldValueWithIndex(string fieldName, object obj, int index, object value,
            bool includeAllBases = false,
            BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                    BindingFlags.NonPublic)
        {
            var field = obj.GetType().GetField(fieldName, bindings);
            if (field != null)
            {
                var list = field.GetValue(obj);
                if (list.GetType().IsArray)
                {
                    ((object[])list)[index] = value;
                    return true;
                }
                else if (list is IEnumerable)
                {
                    ((IList)list)[index] = value;
                    return true;
                }
            }
            return false;
        }
    }
}
