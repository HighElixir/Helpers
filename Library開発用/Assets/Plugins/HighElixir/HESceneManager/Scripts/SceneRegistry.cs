using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.ResourceProviders;
using HighElixir.HESceneManager.Utils;

namespace HighElixir.HESceneManager
{
    public sealed class SceneRegistry
    {
        // CompatID をキーにしてシーンデータを保持する
        private readonly Dictionary<string, SceneData> _loadedScenes = new();

        // 現在アクティブなシーン
        public SceneData CurrentScene { get; private set; } = SceneData.InvalidData();

        internal void RegisterScene(SceneData sceneData, bool shouldOverwrite = true)
        {
            if (sceneData == null || sceneData.IsInvalid()) return;

            var key = sceneData.EnsureCompatID();
            if (string.IsNullOrEmpty(key)) return;

            // 以前このSceneDataを登録したキーがあるなら、ID変化に備えて差し替える
            if (!string.IsNullOrEmpty(sceneData.RegistryKey) && sceneData.RegistryKey != key)
            {
                if (_loadedScenes.TryGetValue(sceneData.RegistryKey, out var oldRef) && ReferenceEquals(oldRef, sceneData))
                    _loadedScenes.Remove(sceneData.RegistryKey);
                sceneData.RegistryKey = "";
            }

            if (_loadedScenes.TryGetValue(key, out var existing))
            {
                // 同じ参照ならキーだけ更新して終わり
                if (ReferenceEquals(existing, sceneData))
                {
                    sceneData.RegistryKey = key;
                    return;
                }

                if (shouldOverwrite && !existing.Equals(sceneData))
                {
                    existing.IsPersisted = sceneData.IsPersisted;

                    if (!sceneData.Scene.IsInvalid() && !sceneData.Scene.Equals(existing.Scene))
                        existing.Scene = sceneData.Scene;

                    if (!sceneData.Instance.IsInvalid() && !sceneData.Instance.Equals(existing.Instance))
                        existing.Instance = sceneData.Instance;

                    if (!string.IsNullOrEmpty(sceneData.AddressableName) && existing.AddressableName != sceneData.AddressableName)
                        existing.AddressableName = sceneData.AddressableName;

                    // 新情報でLinkが成立してCompatIDが変わる可能性があるので再確定
                    var newKey = existing.EnsureCompatID();
                    if (!string.IsNullOrEmpty(newKey) && newKey != key)
                    {
                        _loadedScenes.Remove(key);
                        _loadedScenes[newKey] = existing;
                        existing.RegistryKey = newKey;
                        if (IsCurrentScene(existing)) CurrentScene = existing;
                        return;
                    }
                }

                existing.RegistryKey = key;
                return;
            }

            _loadedScenes.Add(key, sceneData);
            sceneData.RegistryKey = key;
        }

        internal void RegisterScenes(params SceneData[] sceneDatas)
        {
            if (sceneDatas == null) return;
            foreach (var sd in sceneDatas) RegisterScene(sd);
        }

        internal void UnregisterScene(SceneData sceneData)
        {
            if (sceneData == null) return;

            // 登録時のキーで確実に削除
            if (!string.IsNullOrEmpty(sceneData.RegistryKey))
                _loadedScenes.Remove(sceneData.RegistryKey);

            // 念のため現IDでも削除（ID変化したケース対策）
            if (!string.IsNullOrEmpty(sceneData.CompatID))
                _loadedScenes.Remove(sceneData.CompatID);

            if (IsCurrentScene(sceneData))
                CurrentScene = SceneData.InvalidData();

            sceneData.RegistryKey = "";
        }

        internal bool TryMarge(Scene scene)
        {
            if (TryGetScene(scene, out var existing))
            {
                if (!existing.Scene.Equals(scene))
                    existing.Scene = scene;
                return true;
            }
            return false;
        }

        internal bool TryMarge(SceneInstance instance)
        {
            if (TryGetScene(instance, out var existing))
            {
                if (!existing.Instance.Equals(instance))
                    existing.Instance = instance;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 引数が「CompatID」でも「AddressableName」でも「SceneName」でも取れるようにする。
        /// ただし未登録の場合は新規発行しない（TryGetのみ）
        /// </summary>
        public bool TryGetScene(string keyOrName, out SceneData sceneData)
        {
            sceneData = SceneData.InvalidData();
            if (string.IsNullOrEmpty(keyOrName)) return false;

            // 1) まず CompatID として直引き
            if (_loadedScenes.TryGetValue(keyOrName, out sceneData))
                return true;

            // 2) addressableName -> id（既存のみ）
            if (AddressablesCompat.TryGetIDFromAddressable(keyOrName, out var idA) &&
                _loadedScenes.TryGetValue(idA, out sceneData))
                return true;

            // 3) sceneName -> id（既存のみ）
            if (AddressablesCompat.TryGetIDFromSceneName(keyOrName, out var idS) &&
                _loadedScenes.TryGetValue(idS, out sceneData))
                return true;

            sceneData = SceneData.InvalidData();
            return false;
        }

        public bool TryGetScene(Scene scene, out SceneData sceneData)
            => TryGetScene(scene.name, out sceneData);

        public bool TryGetScene(SceneInstance instance, out SceneData sceneData)
            => TryGetScene(instance.Scene.name, out sceneData);

        public bool IsCurrentScene(SceneData sceneData)
            => CurrentScene != null && CurrentScene.Equals(sceneData);

        public IEnumerable<SceneData> GetAllRegisteredScenes()
            => _loadedScenes.Values;

        public void SetCurrentScene(SceneData sceneData)
        {
            if (sceneData == null || sceneData.IsInvalid()) return;

            // CurrentScene設定前に登録整合性を確保
            RegisterScene(sceneData, shouldOverwrite: true);
            CurrentScene = sceneData;
        }
    }
}
