using Cysharp.Threading.Tasks;
using HighElixir.Tweenworks.Internal;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

namespace HighElixir.Tweenworks
{
    public sealed class TweenHolderMono : MonoBehaviour
    {
        [SerializeField]
        private List<TweenProfiler> _profilers = new();

        [SerializeField]
        [Tooltip("Awake時点でnullの場合、自動的にアタッチされたオブジェクトを使用する")]
        private GameObject _target;
        public List<string> GetProfierName()
        {
            return _profilers.ConvertAll<string>(x => x.Name);
        }

        public async UniTask Invoke(string profilerName, Action onComplete)
        {
            if (TryGet(profilerName, out var profiler))
            {
                await profiler.Invoke(onComplete);
            }
        }

        public void Pause(string profilerName, bool pause)
        {
            if (TryGet(profilerName, out var profiler))
            {
                profiler.Pause(pause);
            }
        }

        public void PauseAll(bool pause)
        {
            foreach (var profiler in _profilers)
            {
                profiler.Pause(pause);
            }
        }
        public void Stop(string profilerName)
        {
            if (TryGet(profilerName, out var profiler))
            {
                profiler.Stop();
            }
        }
        public void StopAll()
        {
            foreach (var profiler in _profilers)
            {
                profiler.Stop();
            }
        }

        public void BindTarget(GameObject target)
        {
            _target = target;
            foreach (var item in _profilers)
            {
                item.Bind(target);
            }
        }
        private bool TryGet(string profilerName, out TweenProfiler profiler)
        {
            profiler = _profilers.Find(x => x.Name == profilerName);
            return profiler != null;
        }

        // Unity
        private void OnApplicationPause(bool pause)
        {
            PauseAll(pause);
        }
        private void Awake()
        {
            if (_profilers == null || _profilers.Count <= 0)
            {
                Destroy(gameObject);
                return;
            }
            BindTarget(_target);
        }
        private void OnDestroy()
        {
            foreach(var profiler in _profilers)
            {
                profiler.Dispose();
            }
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_profilers == null)
            {
                _profilers = new();
                return;
            }
            if (_target == null) _target = gameObject;
            CheckValue();
            if (Application.isPlaying)
                BindTarget(_target);
        }
        private void CheckValue()
        {
            Debug.Log("!!!");
            var dic = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // 大文字小文字無視したいなら

            foreach (var item in _profilers)
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                    item.Name = "New Profiler";

                var key = item.Name.RemovePlusNumberTags().Trim();
                Debug.Log($"key:{key}, name:{item.Name}");
                if (!dic.TryAdd(key, 0))
                {
                    dic[key]++;
                    item.Name = $"{key}({dic[key]})";
                }
            }
        }
#endif
    }
}