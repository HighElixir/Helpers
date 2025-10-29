using UnityEngine;

namespace HighElixir
{
    public static class ScreenHelpers
    {
        public static Vector2 WorldToUILocalPos(Vector3 worldPos, Camera camera, Canvas canvas)
        {
            var screenPos = RectTransformUtility.WorldToScreenPoint(camera, worldPos);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.transform as RectTransform,
                    screenPos,
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : camera,
                    out var localPos
                ))
            {
                return localPos;
            }
            else
            {
                Debug.LogWarning("スクリーン→ローカル変換に失敗したよ！");
                return Vector2.zero;
            }
        }
    }
}