using UnityEngine;

namespace HighElixir.Unity.Animations
{
    public static class AnimationValidator
    {
        public static bool IsValid(this Animator animator, AnimatorControllerParameterType type, int hash)
        {
            var pars = animator.parameters;
            for (int i = 0; i < pars.Length; i++)
            {
                var p = pars[i];
                if (p.type == type && p.nameHash == hash) return true;
            }
            return false;
        }
        public static bool IsValid(this Animator animator, AnimatorControllerParameterType type, string name)
        {
            return IsValid(animator, type, Animator.StringToHash(name));
        }
    }
}