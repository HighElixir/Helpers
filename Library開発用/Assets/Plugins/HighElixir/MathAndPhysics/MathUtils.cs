using System;
using Unity.Burst;
using UnityEngine;

namespace HighElixir.Math
{
    [BurstCompile]
    public readonly struct MathUtils
    {
        /// <summary>
        /// 計算結果の丸め桁数を指定します。
        /// 負の値: 小数点以下の桁数（-5 = 小数点以下5桁）
        /// 0: 整数
        /// 正の値: 小数点以上の桁数
        /// </summary>
        private readonly int _precision;

        public int Precision => _precision;

        public MathUtils(int precision = -5)
        {
            _precision = precision;
        }

        public readonly float RoundToDecimalPlaces(float value)
        {
            // precision が負なら小数点以下、正なら小数点以上
            int digits = -_precision;

            return (float)System.Math.Round(value, digits, MidpointRounding.AwayFromZero);
        }

        #region 変換関数
        #region Sinの変換
        public float SinToCos(float sinValue)
        {
            float result = (float)Mathf.Sqrt(SinToCosNotSquared(sinValue));
            return RoundToDecimalPlaces(result);
        }

        public float SinToCosNotSquared(float sinValue)
        {
            float result = 1 - sinValue * sinValue;
            return RoundToDecimalPlaces(result);
        }

        public float SinToTan(float sinValue)
        {
            float result = (float)Mathf.Sqrt(SinToTanNotSquared(sinValue));
            return RoundToDecimalPlaces(result);
        }

        public float SinToTanNotSquared(float sinValue)
        {
            float result = sinValue * sinValue / (1 - sinValue * sinValue);
            return RoundToDecimalPlaces(result);
        }
        #endregion

        #region Cosの変換
        public float CosToSin(float cosValue)
        {
            float result = (float)Mathf.Sqrt(CosToSinNotSquared(cosValue));
            return RoundToDecimalPlaces(result);
        }
        public float CosToSinNotSquared(float cosValue)
        {
            float result = 1 - cosValue * cosValue;
            return RoundToDecimalPlaces(result);
        }
        public float CosToTan(float cosValue)
        {
            float result = (float)Mathf.Sqrt(CosToTanNotSquared(cosValue));
            return RoundToDecimalPlaces(result);
        }
        public float CosToTanNotSquared(float cosValue)
        {
            float result = cosValue * cosValue / (1 - cosValue * cosValue);
            return RoundToDecimalPlaces(result);
        }
        #endregion

        #region Tanの変換
        public float TanToSin(float tanValue)
        {
            float result = (float)Mathf.Sqrt(TanToSinNotSquared(tanValue));
            return RoundToDecimalPlaces(result);
        }
        public float TanToSinNotSquared(float tanValue)
        {
            float result = tanValue * tanValue / (1 + tanValue * tanValue);
            return RoundToDecimalPlaces(result);
        }
        public float TanToCos(float tanValue)
        {
            float result = (float)Mathf.Sqrt(TanToCosNotSquared(tanValue));
            return RoundToDecimalPlaces(result);
        }
        public float TanToCosNotSquared(float tanValue)
        {
            float result = 1 / (1 + tanValue * tanValue);
            return RoundToDecimalPlaces(result);
        }
        #endregion

        #region 弧度法と度数法の変換
        public float DegreesToRadians(float degrees)
        {
            float result = degrees * ((float)System.Math.PI / 180f);
            return RoundToDecimalPlaces(result);
        }
        public float RadiansToDegrees(float radians)
        {
            float result = radians * (180f / (float)System.Math.PI);
            return RoundToDecimalPlaces(result);
        }
        #endregion

        #endregion

        #region 和積・積和
        public float SinSum(float a, float b, bool isAngle = true)
        {
            if (isAngle)
            {
                float result = (float)(Mathf.Sin(a) + Mathf.Sin(b));
                return RoundToDecimalPlaces(result);
            }
            else
            {
                float result = a + b;
                return RoundToDecimalPlaces(result);
            }
        }
        public float SinProduct(float a, float b, bool isAngle = true)
        {
            if (isAngle)
            {
                float result = 0.5f * (float)(Mathf.Cos(a - b) - Mathf.Cos(a + b));
                return RoundToDecimalPlaces(result);
            }
            else
            {
                float result = a * b;
                return RoundToDecimalPlaces(result);
            }
        }
        #endregion

        public (float newValue, float wrapCount) Wrap(float value, float min, float max)
        {
            float range = max - min;
            if (range <= 0)
            {
                return (min, 0);
            }
            float wrappedValue = value;
            float wrapCount = 0;
            while (wrappedValue < min)
            {
                wrappedValue += range;
                wrapCount -= 1;
            }
            while (wrappedValue >= max)
            {
                wrappedValue -= range;
                wrapCount += 1;
            }
            return (RoundToDecimalPlaces(wrappedValue), RoundToDecimalPlaces(wrapCount));
        }

        public float LerpWithWrap(float start, float end, float t, float min, float max)
        {
            var (wrappedStart, _) = Wrap(start, min, max);
            var (wrappedEnd, _) = Wrap(end, min, max);
            float directDistance = wrappedEnd - wrappedStart;
            float wrappedDistance = (directDistance > 0) ? directDistance - (max - min) : directDistance + (max - min);
            float chosenDistance = (Mathf.Abs(directDistance) <= Mathf.Abs(wrappedDistance)) ? directDistance : wrappedDistance;
            float result = wrappedStart + chosenDistance * t;
            var (finalResult, _) = Wrap(result, min, max);
            return RoundToDecimalPlaces(finalResult);
        }

        public float Ease(float s, float e, float t, EaseType type)
        {
            return s + (e - s) * type.Easing(t);
        }

        public float EaseWithWrap(float s, float e, float t, float min, float max, EaseType type)
        {
            var (wrappedStart, _) = Wrap(s, min, max);
            var (wrappedEnd, _) = Wrap(e, min, max);
            float directDistance = wrappedEnd - wrappedStart;
            float wrappedDistance = (directDistance > 0) ? directDistance - (max - min) : directDistance + (max - min);
            float chosenDistance = (Mathf.Abs(directDistance) <= Mathf.Abs(wrappedDistance)) ? directDistance : wrappedDistance;
            float result = wrappedStart + chosenDistance * type.Easing(t);
            var (finalResult, _) = Wrap(result, min, max);
            return RoundToDecimalPlaces(finalResult);
        }
    }
}
