using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Prg.Anim;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Debug = Prg.Debug;
#if UNITY_EDITOR
using System.Text.RegularExpressions;
#endif

namespace Editor.Prg.EditorSupport
{
    [CustomEditor(typeof(AnimDef))]
    public class AnimDefEditor : UnityEditor.Editor
    {
        private static readonly Regex SplitCamelCase = new(@"[A-Z][a-z]*|[a-z]+|\d+", RegexOptions.Compiled);

        public static Func<string, string> ParseStateNameFunc = ParseStateNameToWords;

        public override void OnInspectorGUI()
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                DrawDefaultInspector();
                return;
            }
            GUILayout.Space(20);
            if (GUILayout.Button("Update Animator State Names"))
            {
                Debug.Log("*");
                serializedObject.Update();
                UpdateState(serializedObject);
                serializedObject.ApplyModifiedProperties();
            }
            GUILayout.Space(20);
            DrawDefaultInspector();
        }

        [Conditional("UNITY_EDITOR")]
        private static void UpdateState(SerializedObject serializedObject)
        {
            var property = serializedObject.FindProperty(AnimDef.AnimatorDefName);
            var animatorProp = property.FindPropertyRelative(AnimatorDef.AnimatorName);
            var stateNamesProp = property.FindPropertyRelative(AnimatorDef.StateNamesName);
            var stateNames = stateNamesProp.stringValue;
#if UNITY_EDITOR
            // Outside Editor this is RuntimeAnimatorController!
            var controller = animatorProp.objectReferenceValue as AnimatorController;
            if (controller != null)
            {
                stateNames = ParseStateNames(controller, 0);
                DumpAnimStates(controller);
            }
#endif
            if (stateNamesProp.stringValue == stateNames)
            {
                return;
            }
            Debug.Log($"{animatorProp.name}");
            Debug.Log($"{animatorProp.objectReferenceValue}");
            Debug.Log($"{stateNamesProp.name}");
            Debug.Log($"{stateNamesProp.stringValue} <- {stateNames}");
            stateNamesProp.stringValue = stateNames;
        }

        public static string ParseStateNameToWords(string stateName)
        {
            if (stateName.Contains(' '))
            {
                return "ERROR:no-space-in-state-name";
            }
            var words = new List<string>();
            foreach (Match match in SplitCamelCase.Matches(stateName))
            {
                words.Add(match.Value);
            }
            return string.Join(' ', words);
        }

#if UNITY_EDITOR
        private static string ParseStateNames(AnimatorController controller, int layer)
        {
            var stateMachine = controller.layers[layer].stateMachine;
            var stateNames = stateMachine.states.Select(x => x.state.name).ToList();
            var uniqueValues = new HashSet<string>();
            var validLines = new List<string>();
            var errorLines = new List<string>();
            foreach (var stateName in stateNames)
            {
                var stateValue = ParseStateNameFunc(stateName);
                if (stateValue.Contains("Error"))
                {
                    errorLines.Add($"{stateName}={stateValue}");
                    continue;
                }
                if (!uniqueValues.Add(stateValue))
                {
                    errorLines.Add($"{stateName}={stateValue}");
                    continue;
                }
                validLines.Add($"{stateName}={stateValue}");
            }
            validLines.Sort();
            if (errorLines.Count == 0)
            {
                return string.Join("\r\n", validLines);
            }
            errorLines.Sort();
            return string.Join("\r\n", errorLines) + "\r\n" + string.Join("\r\n", validLines);
        }

        private static void DumpAnimStates(AnimatorController animatorController)
        {
            foreach (var layer in animatorController.layers)
            {
                var stateMachine = layer.stateMachine;
                var states = stateMachine.states;
                var behaviours = stateMachine.behaviours;
                Debug.Log($"{stateMachine.name} {layer.name} behaviours {behaviours.Length} states {states.Length}");
                foreach (var animatorState in states)
                {
                    var state = animatorState.state;
                    behaviours = state.behaviours;
                    if (behaviours.Length == 0)
                    {
                        state.AddStateMachineBehaviour(typeof(StateMachineListener));
                    }
                    Debug.Log($"{state.nameHash} {state.name} behaviours {behaviours.Length}");
                }
            }
        }

#endif
    }
}
