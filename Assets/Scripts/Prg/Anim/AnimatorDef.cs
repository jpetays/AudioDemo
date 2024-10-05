using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Prg.Anim
{
    /// <summary>
    /// Convenience class to create and save <c>Animator</c> state names in UNITY Editor.
    /// </summary>
    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class AnimatorDef
    {
        public const string AnimatorName = nameof(Animator);
        public const string StateNamesName = nameof(StateNames);

        [SerializeField] private RuntimeAnimatorController Animator;
        public string StateNames;
    }
}
