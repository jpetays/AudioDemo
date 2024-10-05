using UnityEngine;

namespace Prg.Util
{
    /// <summary>
    /// Extension methods for UNITY animators.
    /// </summary>
    public static class AnimatorUtil
    {
        public static Animator[] FindAnimators(this GameObject gameObject)
        {
            return gameObject.GetComponentsInChildren<Animator>(true);
        }

        public static float GetMaxPlayTime(this GameObject gameObject, float curPlayDuration = 0)
        {
            return GetMaxPlayTime(gameObject.FindAnimators(), curPlayDuration);
        }

        public static float GetMaxPlayTime(this Animator[] animators, float curPlayDuration = 0)
        {
            if (animators.Length == 0)
            {
                return curPlayDuration;
            }
            var maxPlayDuration = curPlayDuration;
            foreach (var animator in animators)
            {
                var time = animator.GetMaxPlayTime();
                if (time > maxPlayDuration)
                {
                    maxPlayDuration = time;
                }
            }
            return maxPlayDuration;
        }

        public static float GetMaxPlayTime(this Animator animator)
        {
            var maxPlayDuration = 0f;
            foreach (var animationClip in animator.runtimeAnimatorController.animationClips)
            {
                if (animationClip.length > maxPlayDuration)
                {
                    maxPlayDuration = animationClip.length;
                }
            }
            return maxPlayDuration;
        }

        public static float SetDisabled(this Animator[] animators,
            bool returnMaxPlayDuration, float curPlayDuration = 0)
        {
            if (animators.Length == 0)
            {
                return curPlayDuration;
            }
            var maxPlayDuration = curPlayDuration;
            foreach (var animator in animators)
            {
                animator.enabled = false;
                if (!returnMaxPlayDuration)
                {
                    continue;
                }
                foreach (var animationClip in animator.runtimeAnimatorController.animationClips)
                {
                    if (animationClip.length > maxPlayDuration)
                    {
                        maxPlayDuration = animationClip.length;
                    }
                }
            }
            return maxPlayDuration;
        }

        public static void SetDisabled(this Animator[] animators)
        {
            if (animators.Length == 0)
            {
                return;
            }
            foreach (var animator in animators)
            {
                animator.enabled = false;
            }
        }

        public static void SetEnabled(this Animator[] animators)
        {
            if (animators.Length == 0)
            {
                return;
            }
            foreach (var animator in animators)
            {
                animator.enabled = true;
            }
        }
    }
}
