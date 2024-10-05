using UnityEngine;
#if POODLE_DEUG_ANIM
using System;
using UnityEngine.Animations;
#endif

namespace Prg.Anim
{
    public class StateMachineListener : StateMachineBehaviour
    {
#if POODLE_DEUG_ANIM
        public static Action<int> OnStateEnterCallback;
        public static Action<int> OnStateExitCallback;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            OnStateEnterCallback?.Invoke(stateInfo.shortNameHash);
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex,
            AnimatorControllerPlayable controller)
        {
            OnStateEnterCallback?.Invoke(stateInfo.shortNameHash);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            OnStateExitCallback?.Invoke(stateInfo.shortNameHash);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex,
            AnimatorControllerPlayable controller)
        {
            OnStateExitCallback?.Invoke(stateInfo.shortNameHash);
        }
#endif
    }
}
