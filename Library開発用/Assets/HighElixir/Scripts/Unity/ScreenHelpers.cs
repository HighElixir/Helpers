using UnityEngine;

namespace HighElixir
{
    public static class ScreenHelpers
    {
        public static Vector2 WorldToUILocalPos(Vector3 worldPos, Camera camera, Canvas canvas)
            => WorldToUILocalPos(worldPos, camera, canvas.GetComponent<RectTransform>(), canvas.renderMode);
        public static Vector2 WorldToUILocalPos(Vector3 worldPos, Camera camera, RectTransform parentRect, RenderMode renderMode = RenderMode.ScreenSpaceOverlay)
        {
            var screenPos = camera.WorldToScreenPoint(worldPos);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    screenPos,
                    renderMode == RenderMode.ScreenSpaceOverlay ? null : camera,
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