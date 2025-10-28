using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HighElixir.SearchSystems.Comps
{
    [Serializable]
    public sealed class LookingForward : SearchSystem<GameObject, GameObject>.ISearchComponent
    {
        [Header("視野設定")]
        [SerializeField] private float _distance = 10f;
        [SerializeField] private float _verticalView = 60f;
        [SerializeField] private float _horizontalView = 90f;
        [SerializeField] private LayerMask _layerMask;

        [Header("視点設定")]
        [SerializeField] private Vector3 _eyeOffset = Vector3.zero;
        [SerializeField] private Vector3 _eyeAngleOffset = Vector3.zero;

        [Header("オプション")]
        [SerializeField] private bool _reversing = false;
        [SerializeField] private float _deadZone = 0f;
        [SerializeField] private bool _is2D = false;

        public float Distance { get => _distance; set => _distance = value; }
        public float VerticalView { get => _verticalView; set => _verticalView = value; }
        public float HorizontalView { get => _horizontalView; set => _horizontalView = value; }
        public LayerMask LayerMask { get => _layerMask; set => _layerMask = value; }
        public Vector3 EyeOffset { get => _eyeOffset; set => _eyeOffset = value; }
        public Vector3 EyeAngleOffset { get => _eyeAngleOffset; set => _eyeAngleOffset = value; }
        public bool Reversing { get => _reversing; set => _reversing = value; }
        public float DeadZone { get => _deadZone; set => _deadZone = value; }
        public bool Is2D { get => _is2D; set => _is2D = value; }

        public LookingForward(float distance, float verticalView, float horizontalView, LayerMask layerMask, bool is2D = false)
        {
            _distance = distance;
            _verticalView = verticalView;
            _horizontalView = horizontalView;
            _is2D = is2D;
            _layerMask = layerMask;
        }

        public void Dispose() { }

        public void Initialize() { }

        public void ExecuteSearch(GameObject cont, ref List<GameObject> targets)
        {
            var eyePos = cont.transform.position + _eyeOffset;
            var baseRotation = cont.transform.rotation * Quaternion.Euler(_eyeAngleOffset);
            var forward = baseRotation * Vector3.forward;

            if (_is2D)
            {
                // 2Dモード（視野角付き）
                var hits = Physics2D.CircleCastAll(eyePos, _distance, forward, 0f, _layerMask);
                FilterTargets2D(eyePos, forward, hits, ref targets);
            }
            else
            {
                // 3Dモード
                var hits = Physics.SphereCastAll(eyePos, 0.1f, forward, _distance, _layerMask);
                FilterTargets3D(eyePos, forward, hits, ref targets);
            }
        }

        private void FilterTargets2D(Vector2 eyePos, Vector2 forward, RaycastHit2D[] hits, ref List<GameObject> targets)
        {
            var visibleObjects = hits.Select(h => h.collider.gameObject).ToList();

            targets.RemoveAll(target =>
            {
                if (target == null) return true;
                Vector2 dir = ((Vector2)target.transform.position - eyePos).normalized;

                // 視野角チェック
                float angle = Vector2.Angle(forward, dir);
                if (angle > _horizontalView * 0.5f) return !_reversing;

                // DeadZoneチェック
                if (_deadZone > 0 && Vector2.Distance(eyePos, target.transform.position) <= _deadZone)
                    return !_reversing;

                // ヒットチェック
                bool visible = visibleObjects.Contains(target);
                return _reversing ? visible : !visible;
            });
        }

        private void FilterTargets3D(Vector3 eyePos, Vector3 forward, RaycastHit[] hits, ref List<GameObject> targets)
        {
            var visibleObjects = hits.Select(h => h.collider.gameObject).ToList();

            targets.RemoveAll(target =>
            {
                if (target == null) return true;
                Vector3 dir = (target.transform.position - eyePos).normalized;

                // 水平方向・垂直方向の視野角チェック
                float horizontalAngle = Vector3.Angle(Vector3.ProjectOnPlane(forward, Vector3.up), Vector3.ProjectOnPlane(dir, Vector3.up));
                float verticalAngle = Vector3.Angle(forward, dir);

                if (horizontalAngle > _horizontalView * 0.5f || verticalAngle > _verticalView * 0.5f)
                    return !_reversing;

                // DeadZoneチェック
                if (_deadZone > 0 && Vector3.Distance(eyePos, target.transform.position) <= _deadZone)
                    return !_reversing;

                // Raycastで実際に見えてるかチェック
                bool visible = visibleObjects.Contains(target);
                return _reversing ? visible : !visible;
            });
        }
    }
}
