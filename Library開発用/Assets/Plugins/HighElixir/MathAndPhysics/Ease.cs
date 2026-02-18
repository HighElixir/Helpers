using System;
using Unity.Burst;
using UnityEngine;

namespace HighElixir.Math
{
    public enum EaseType : byte
    {
        Linear,
        // sine
        InSine, OutSine, InOutSine,
        // quad
        InQuad, OutQuad, InOutQuad,
        // cubic
        InCubic, OutCubic, InOutCubic,
        // quart
        InQuart, OutQuart, InOutQuart,
        // quint
        InQuint, OutQuint, InOutQuint,
        // expo
        InExpo, OutExpo, InOutExpo,
        // circ
        InCirc, OutCirc, InOutCirc,
        // back
        InBack, OutBack, InOutBack,
        // elastic
        InElastic, OutElastic, InOutElastic,
        // bounce
        InBounce, OutBounce, InOutBounce,
    }

    [BurstCompile]
    public static class Ease
    {
        #region Ease取得関数
        public static float Easing(this EaseType easeType, float t)
        {
            return easeType switch
            {
                EaseType.Linear => Linear(t),
                // sine
                EaseType.InSine => InSine(t),
                EaseType.OutSine => OutSine(t),
                EaseType.InOutSine => InOutSine(t),
                // quad
                EaseType.InQuad => InQuad(t),
                EaseType.OutQuad => OutQuad(t),
                EaseType.InOutQuad => InOutQuad(t),
                // cubic
                EaseType.InCubic => InCubic(t),
                EaseType.OutCubic => OutCubic(t),
                EaseType.InOutCubic => InOutCubic(t),
                // quart
                EaseType.InQuart => InQuart(t),
                EaseType.OutQuart => OutQuart(t),
                EaseType.InOutQuart => InOutQuart(t),
                // quint
                EaseType.InQuint => InQuint(t),
                EaseType.OutQuint => OutQuint(t),
                EaseType.InOutQuint => InOutQuint(t),
                // expo
                EaseType.InExpo => InExpo(t),
                EaseType.OutExpo => OutExpo(t),
                EaseType.InOutExpo => InOutExpo(t),
                // circ
                EaseType.InCirc => InCirc(t),
                EaseType.OutCirc => OutCirc(t),
                EaseType.InOutCirc => InOutCirc(t),
                // back
                EaseType.InBack => InBack(t),
                EaseType.OutBack => OutBack(t),
                EaseType.InOutBack => InOutBack(t),
                // elastic
                EaseType.InElastic => InElastic(t),
                EaseType.OutElastic => OutElastic(t),
                EaseType.InOutElastic => InOutElastic(t),
                // bounce
                EaseType.InBounce => InBounce(t),
                EaseType.OutBounce => OutBounce(t),
                EaseType.InOutBounce => InOutBounce(t),
                _ => Linear(t),
            };
        }

        public static float EasingClamped(this EaseType type, float t)
        {
            t = Mathf.Clamp01(t);
            return type.Easing(t);
        }

        public static float EasingWithString(string param, float t)
        {
            if (Enum.TryParse<EaseType>(param, true, out var ease))
                return Easing(ease, t);
            return t;
        }

        public static float ClampedEasingWithString(string param, float t)
        {
            t = Mathf.Clamp01(t);
            return EasingWithString(param, t);
        }
        #endregion

        #region Ease関数
        public static float Linear(float t)
        {
            return t;
        }

        #region Sine
        public static float InSine(float t)
        {
            return 1 - (float)Mathf.Cos((t * Mathf.PI) / 2);
        }

        public static float OutSine(float t)
        {
            return (float)Mathf.Sin((t * Mathf.PI) / 2);
        }

        public static float InOutSine(float t)
        {
            return -0.5f * ((float)Mathf.Cos(Mathf.PI * t) - 1);
        }
        #endregion

        #region Quad
        public static float InQuad(float t)
        {
            return Mathf.Pow(t, 2);
        }

        public static float OutQuad(float t)
        {
            return OutPow(t, 2);
        }

        public static float InOutQuad(float t)
        {
            return InOutPow(t, 2);
        }
        #endregion

        #region Cubic
        public static float InCubic(float t)
        {
            return Mathf.Pow(t, 3);
        }

        public static float OutCubic(float t)
        {
            return OutPow(t, 3);
        }

        public static float InOutCubic(float t)
        {
            return InOutPow(t, 3);
        }
        #endregion

        #region Quart
        public static float InQuart(float t)
        {
            return Mathf.Pow(t, 4);
        }

        public static float OutQuart(float t)
        {
            return OutPow(t, 4);
        }

        public static float InOutQuart(float t)
        {
            return InOutPow(t, 4);
        }
        #endregion

        #region Quint
        public static float InQuint(float t)
        {
            return Mathf.Pow(t, 5);
        }

        public static float OutQuint(float t)
        {
            return OutPow(t, 5);
        }

        public static float InOutQuint(float t)
        {
            return InOutPow(t, 5);
        }
        #endregion

        #region Expo
        public static float InExpo(float t)
        {
            return t == 0 ? 0 : Mathf.Pow(2, 10 * t - 10);
        }

        public static float OutExpo(float t)
        {
            return t == 1 ? 1 : 1 - Mathf.Pow(2, -10 * t);
        }

        public static float InOutExpo(float t)
        {
            return t == 0
                ? 0
                : t == 1
                ? 1
                : t < 0.5 ? Mathf.Pow(2, 20 * t - 10) / 2
                : (2 - Mathf.Pow(2, -20 * t + 10)) / 2;
        }
        #endregion

        #region Circ
        public static float InCirc(float t)
        {
            return 1 - (float)Mathf.Sqrt(1 - t * t);
        }

        public static float OutCirc(float t)
        {
            t -= 1;
            return (float)Mathf.Sqrt(1 - t * t);
        }

        public static float InOutCirc(float t)
        {
            return t < 0.5f
                ? (1 - (float)Mathf.Sqrt(1 - 4 * t * t)) / 2
                : ((float)Mathf.Sqrt(1 - (2 * t - 2) * (2 * t - 2)) + 1) / 2;
        }
        #endregion

        #region Back
        public static float InBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;
            return c3 * Mathf.Pow(t, 3) - c1 * Mathf.Pow(t, 2);
        }

        public static float OutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;
            return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
        }

        public static float InOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c2 = c1 * 1.525f;
            return t < 0.5
              ? (Mathf.Pow(2 * t, 2) * ((c2 + 1) * 2 * t - c2)) / 2
              : (Mathf.Pow(2 * t - 2, 2) * ((c2 + 1) * (t * 2 - 2) + c2) + 2) / 2;
        }
        #endregion

        #region Elastic
        public static float InElastic(float t)
        {
            const float c4 = (2 * Mathf.PI) / 3;

            return t == 0
              ? 0
              : t == 1
              ? 1
              : -Mathf.Pow(2, 10 * t - 10) * Mathf.Sin((t * 10 - 10.75f) * c4);
        }

        public static float OutElastic(float t)
        {
            const float c4 = (2 * Mathf.PI) / 3;

            return t == 0
              ? 0
              : t == 1
              ? 1
              : Mathf.Pow(2, -10 * t) * Mathf.Sin((t * 10 - 0.75f) * c4) + 1;
        }

        public static float InOutElastic(float t)
        {
            const float c5 = (2 * Mathf.PI) / 4.5f;

            return t == 0
              ? 0
              : t == 1
              ? 1
              : t < 0.5
              ? -(Mathf.Pow(2, 20 * t - 10) * Mathf.Sin((20 * t - 11.125f) * c5)) / 2
              : (Mathf.Pow(2, -20 * t + 10) * Mathf.Sin((20 * t - 11.125f) * c5)) / 2 + 1;
        }
        #endregion

        #region Bounce
        public static float InBounce(float t)
        {
            return 1 - OutBounce(1 - t);
        }

        public static float OutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1 / d1)
            {
                return n1 * Mathf.Pow(t, 2);
            }
            else if (t < 2 / d1)
            {
                return n1 * ((t - 1.5f) / d1) * t + 0.75f;
            }
            else if (t < 2.5 / d1)
            {
                return n1 * ((t - 2.25f) / d1) * t + 0.9375f;
            }
            else
            {
                return n1 * ((t -= 2.625f) / d1) * t + 0.984375f;
            }
        }

        public static float InOutBounce(float t)
        {
            return t < 0.5
                ? (1 - OutBounce(1 - 2 * t)) / 2
                : (1 + OutBounce(2 * t - 1)) / 2;
        }
        #endregion

        #region Private Methodes
        private static float OutPow(float t, int cont)
        {
            var rv = 1 - t;
            return 1 - Mathf.Pow(rv, cont);
        }
        private static float InOutPow(float t, int cont)
        {
            return t < 0.5 ? Mathf.Pow(2, cont - 1) * Mathf.Pow(t, cont) : 1 - Mathf.Pow(-2 * t + 2, cont) / 2;
        }
        #endregion
        #endregion
    }
}