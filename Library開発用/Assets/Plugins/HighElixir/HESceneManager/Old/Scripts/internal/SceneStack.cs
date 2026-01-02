using System;
using System.Collections.Concurrent;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace HighElixir.Unity.Addressable.SceneManagement.Internal
{
    internal static class SceneStack
    {
        private static ConcurrentDictionary<Scene, SceneInstance> _scenes = new();
        private static ConcurrentDictionary<string, SceneInstance> _assetToScene = new();
        private static ConcurrentDictionary<SceneInstance, string> _sceneToAsset = new();
        private static ConcurrentStack<SceneInstance> _sceneStack = new();
        private static SceneInstance _currentScene = Invalid;

        public static SceneInstance CurrentScene => _currentScene;

        private static readonly SceneInstance Invalid = default;
        public static void RegisterScene(SceneInstance scene, string key)
        {
            _scenes.TryAdd(scene.Scene, scene);
            _assetToScene.TryAdd(key, scene);
            _sceneToAsset.TryAdd(scene, key);
        }

        public static void UnregisterScene(SceneInstance scene)
        {
            _scenes.TryRemove(scene.Scene, out _);
            if (_sceneToAsset.TryGetValue(scene, out var r))
                _assetToScene.TryRemove(r, out _);
        }

        public static void UnregisterScene(string key)
        {
            if (_assetToScene.TryGetValue(key, out var scene))
                UnregisterScene(scene);
        }
        public static SceneInstance GetScene(Scene scene)
        {
            if (_scenes.TryGetValue(scene, out var sceneInstance))
            {
                return sceneInstance;
            }
            throw new ArgumentException($"{scene.name} do not registered in Stack");
        }
        public static bool TryGetScene(Scene scene, out SceneInstance instance)
        {
            return _scenes.TryGetValue(scene, out instance);
        }

        public static bool TryGetScene(string scene, out SceneInstance instance)
        {
            return _assetToScene.TryGetValue(scene, out instance);
        }

        public static SceneInstance GetCurrentSceneInstance()
        {
            var active = SceneManager.GetActiveScene();
            return GetScene(active);
        }

        public static bool TryGetCurrentSceneInstance(out SceneInstance instance)
        {
            var active = SceneManager.GetActiveScene();
            return TryGetScene(active, out instance);
        }

        #region 前のシーンに戻るなどの処理

        public static void Push(SceneInstance scene)
        {
            if (!_scenes.Equals(Invalid))
            {
                _sceneStack.Push(_currentScene);
            }
            _currentScene = scene;
        }

        public static bool TryPop(out SceneInstance reference)
        {
            if (_sceneStack.TryPop(out reference))
            {
                _currentScene = reference;
                return true;
            }
            return false;
        }

        #endregion

        public static void Clear()
        {
            _scenes.Clear();
            _sceneStack.Clear();
            _currentScene = Invalid;
        }

    }
}