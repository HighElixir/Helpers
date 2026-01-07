using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

namespace HighElixir.HESceneManager.DirectionSequence
{
    /// <summary>
    /// シーケンス内で SceneData を保存して参照するための棚。
    /// </summary>
    public sealed class SceneStore
    {
        private readonly Dictionary<string, SceneData> _map = new();

        public void Set(string key, SceneData data)
        {
            if (string.IsNullOrEmpty(key) || data == null) return;
            _map[key] = data;
        }

        public bool TryGet(string key, out SceneData data)
        {
            data = null;
            if (string.IsNullOrEmpty(key)) return false;
            return _map.TryGetValue(key, out data) && data != null && !data.IsInvalid();
        }

        public bool Remove(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return _map.Remove(key);
        }

        public void Clear()
        {
            _map.Clear();
        }
    }

    // ---------------- Actions ----------------

    /// <summary>
    /// Addressablesシーンをロードし、Storeに保存する。
    /// </summary>
    public sealed class LoadSceneAction : IAction
    {
        public float Time { get; set; }
        public float Duration { get; set; } = 0f;

        private readonly SceneService _service;
        private readonly SceneStore _store;
        private readonly string _storeKey;
        private readonly string _addressableName;
        private readonly bool _persisted;

        public LoadSceneAction(SceneService service, SceneStore store, string storeKey, string addressableName, bool persisted)
        {
            _service = service;
            _store = store;
            _storeKey = storeKey;
            _addressableName = addressableName;
            _persisted = persisted;
        }

        public async UniTask DO(CancellationToken token)
        {
            if (_service == null || _store == null) return;
            if (string.IsNullOrEmpty(_addressableName)) return;

            var sd = await _service.LoadSceneAsync(_addressableName);
            if (sd == null || sd.IsInvalid()) return;

            // ここで保存して、後から Activate/Unload できるようにする
            sd.IsPersisted = _persisted;
            _store.Set(_storeKey, sd);
        }
    }

    /// <summary>
    /// StoreKey（またはRegistryキー）から SceneData を探してアクティベートする。
    /// </summary>
    public sealed class ActivateSceneAction : IAction
    {
        public float Time { get; set; }
        public float Duration { get; set; } = 0f;

        private readonly SceneService _service;
        private readonly SceneStore _store;
        private readonly string _keyOrStoreKey;

        public ActivateSceneAction(SceneService service, SceneStore store, string keyOrStoreKey)
        {
            _service = service;
            _store = store;
            _keyOrStoreKey = keyOrStoreKey;
        }

        public async UniTask DO(CancellationToken token)
        {
            if (_service == null) return;
            if (string.IsNullOrEmpty(_keyOrStoreKey)) return;

            SceneData sd = null;

            // 1) まずStore参照
            if (_store != null && _store.TryGet(_keyOrStoreKey, out var stored))
                sd = stored;
            // 2) ダメならRegistry参照（CompatID / addressableName / sceneName でもOK想定）
            else if (_service.Registry.TryGetScene(_keyOrStoreKey, out var reg))
                sd = reg;

            if (sd == null || sd.IsInvalid()) return;

            await _service.ActivateAsync(sd);
        }
    }

    /// <summary>
    /// StoreKey（またはRegistryキー）から SceneData を探してアンロードする。
    /// </summary>
    public sealed class UnloadSceneAction : IAction
    {
        public float Time { get; set; }
        public float Duration { get; set; } = 0f;

        private readonly SceneService _service;
        private readonly SceneStore _store;
        private readonly string _keyOrStoreKey;
        private readonly bool _disposeResource;
        private readonly bool _removeFromStore;

        public UnloadSceneAction(SceneService service, SceneStore store, string keyOrStoreKey, bool disposeResource, bool removeFromStore)
        {
            _service = service;
            _store = store;
            _keyOrStoreKey = keyOrStoreKey;
            _disposeResource = disposeResource;
            _removeFromStore = removeFromStore;
        }

        public async UniTask DO(CancellationToken token)
        {
            if (_service == null) return;
            if (string.IsNullOrEmpty(_keyOrStoreKey)) return;

            SceneData sd = null;

            if (_store != null && _store.TryGet(_keyOrStoreKey, out var stored))
                sd = stored;
            else if (_service.Registry.TryGetScene(_keyOrStoreKey, out var reg))
                sd = reg;

            if (sd == null || sd.IsInvalid()) return;

            await _service.UnloadAsync(sd, disposeResource: _disposeResource);

            if (_removeFromStore && _store != null)
                _store.Remove(_keyOrStoreKey);
        }
    }

    /// <summary>
    /// Storeに指定キーが入るまで待つ（Joinでロード並列した時の“後続”に便利）。
    /// </summary>
    public sealed class WaitForStoredSceneAction : IAction
    {
        public float Time { get; set; }
        public float Duration { get; set; } = 0f;

        private readonly SceneStore _store;
        private readonly string _storeKey;
        private readonly float _timeoutSeconds;

        public WaitForStoredSceneAction(SceneStore store, string storeKey, float timeoutSeconds = -1f)
        {
            _store = store;
            _storeKey = storeKey;
            _timeoutSeconds = timeoutSeconds;
        }

        public async UniTask DO(CancellationToken token)
        {
            if (_store == null || string.IsNullOrEmpty(_storeKey)) return;

            if (_timeoutSeconds > 0f)
            {
                // タイムアウト付き（必要なら）
                var until = UniTask.WaitUntil(() => _store.TryGet(_storeKey, out _), cancellationToken: token);
                var timeout = UniTask.Delay(TimeSpan.FromSeconds(_timeoutSeconds), cancellationToken: token);
                var win = await UniTask.WhenAny(until, timeout);
                // win==0 なら到達、win==1 ならタイムアウト（ここでは黙って抜ける）
                return;
            }

            await UniTask.WaitUntil(() => _store.TryGet(_storeKey, out _), cancellationToken: token);
        }
    }
}
