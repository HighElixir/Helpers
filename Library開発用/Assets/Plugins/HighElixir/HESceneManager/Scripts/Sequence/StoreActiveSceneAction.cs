using Cysharp.Threading.Tasks;
using HighElixir.HESceneManager.Utils;
using System.Threading;

namespace HighElixir.HESceneManager.DirectionSequence
{
    /// <summary>
    /// その時点のアクティブシーンを Store に保存する（Duration=0）。
    /// </summary>
    public sealed class StoreActiveSceneAction : IAction
    {
        public float Time { get; set; }
        public float Duration { get; set; } = 0f;

        private readonly SceneService _service;
        private readonly SceneStore _store;
        private readonly string _storeKey;

        public StoreActiveSceneAction(SceneService service, SceneStore store, string storeKey)
        {
            _service = service;
            _store = store;
            _storeKey = storeKey;
        }

        public UniTask DO(CancellationToken token)
        {
            if (_service == null || _store == null) return UniTask.CompletedTask;

            var active = _service.GetOrRegisterActiveSceneData();
            if (active != null && !active.IsInvalid())
                _store.Set(_storeKey, active);

            return UniTask.CompletedTask;
        }
    }

    /// <summary>
    /// Registry からキー/名前で探して Store に保存する（Duration=0）。
    /// </summary>
    public sealed class StoreSceneFromRegistryAction : IAction
    {
        public float Time { get; set; }
        public float Duration { get; set; } = 0f;

        private readonly SceneService _service;
        private readonly SceneStore _store;
        private readonly string _storeKey;
        private readonly string _keyOrName;

        public StoreSceneFromRegistryAction(SceneService service, SceneStore store, string storeKey, string keyOrName)
        {
            _service = service;
            _store = store;
            _storeKey = storeKey;
            _keyOrName = keyOrName;
        }

        public UniTask DO(CancellationToken token)
        {
            if (_service == null || _store == null) return UniTask.CompletedTask;

            if (_service.Registry.TryGetScene(_keyOrName, out var sd))
                _store.Set(_storeKey, sd);

            return UniTask.CompletedTask;
        }
    }

    public static class SequenceStoreActionExtensions
    {
        public static Sequence AppendStoreActive(this Sequence seq, string storeKey)
            => seq.Append(new StoreActiveSceneAction(seq.Service, seq.Store, storeKey));

        public static Sequence JoinStoreActive(this Sequence seq, string storeKey, float offset = 0f)
            => seq.Join(new StoreActiveSceneAction(seq.Service, seq.Store, storeKey), offset);

        public static Sequence AppendStoreFromRegistry(this Sequence seq, string storeKey, string keyOrName)
            => seq.Append(new StoreSceneFromRegistryAction(seq.Service, seq.Store, storeKey, keyOrName));

        public static Sequence JoinStoreFromRegistry(this Sequence seq, string storeKey, string keyOrName, float offset = 0f)
            => seq.Join(new StoreSceneFromRegistryAction(seq.Service, seq.Store, storeKey, keyOrName), offset);
    }
}
