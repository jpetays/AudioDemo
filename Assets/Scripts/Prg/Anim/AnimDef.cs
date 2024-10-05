using UnityEngine;

namespace Prg.Anim
{
    /// <summary>
    /// <c>Animator</c> configuration helper.
    /// </summary>
    [CreateAssetMenu(menuName = "Prg/Prg/AnimDef", fileName = "anim NAME")]
    public class AnimDef : ScriptableObject
    {
        public const string AnimatorDefName = nameof(_animatorDef);

        [SerializeField] private AnimatorDef _animatorDef;

        public string StateNames => _animatorDef.StateNames;
    }
}
