using HighElixir.HESceneManager.Utils;

namespace HighElixir.HESceneManager.DirectionSequence
{
    /// <summary>
    /// Sequence の拡張メソッドを全部まとめた統合クラス。
    /// - Scene操作（Load/Activate/Unload/WaitStored）
    /// - Store操作（即登録 + Play中登録）
    /// </summary>
    public static class SequenceExtensions
    {
        // ---------------------------------------------------------------------
        // Store (Immediate) : その場で Store に入れる（シーケンス再生を待たない）
        // ---------------------------------------------------------------------

        /// <summary>
        /// 任意の SceneData を Store に即保存。
        /// </summary>
        public static Sequence StoreScene(this Sequence seq, string storeKey, SceneData sceneData)
        {
            if (seq == null) return seq;
            if (sceneData == null || sceneData.IsInvalid()) return seq;

            seq.Store.Set(storeKey, sceneData);
            return seq;
        }

        /// <summary>
        /// Registry からキー/名前で探して Store に即保存。
        /// keyOrName は CompatID / addressableName / sceneName のどれでもOK想定。
        /// </summary>
        public static Sequence StoreSceneFromRegistry(this Sequence seq, string storeKey, string keyOrName)
        {
            if (seq == null) return seq;
            if (seq.Service == null) return seq;

            if (seq.Service.Registry.TryGetScene(keyOrName, out var sd))
                seq.Store.Set(storeKey, sd);

            return seq;
        }

        /// <summary>
        /// 現在アクティブなシーンを Store に即保存（Registry優先で取得/登録）。
        /// </summary>
        public static Sequence StoreActiveScene(this Sequence seq, string storeKey)
        {
            if (seq == null) return seq;
            if (seq.Service == null) return seq;

            var active = seq.Service.GetOrRegisterActiveSceneData();
            if (active != null && !active.IsInvalid())
                seq.Store.Set(storeKey, active);

            return seq;
        }

        // ---------------------------------------------------------------------
        // Scene Actions (Append) : 直列
        // ---------------------------------------------------------------------

        public static Sequence AppendLoad(this Sequence seq, string storeKey, string addressableName, bool persisted = false)
            => seq.Append(new LoadSceneAction(seq.Service, seq.Store, storeKey, addressableName, persisted));

        public static Sequence AppendActivate(this Sequence seq, string keyOrStoreKey)
            => seq.Append(new ActivateSceneAction(seq.Service, seq.Store, keyOrStoreKey));

        public static Sequence AppendUnload(this Sequence seq, string keyOrStoreKey, bool disposeResource = false, bool removeFromStore = true)
            => seq.Append(new UnloadSceneAction(seq.Service, seq.Store, keyOrStoreKey, disposeResource, removeFromStore));

        /// <summary>
        /// Store に指定キーが入るまで待つ（ロードの完了同期などに使う）。
        /// </summary>
        public static Sequence AppendWaitStored(this Sequence seq, string storeKey, float timeoutSeconds = -1f)
            => seq.Append(new WaitForStoredSceneAction(seq.Store, storeKey, timeoutSeconds));

        // ---------------------------------------------------------------------
        // Scene Actions (Join) : 並列（Sequence側が UniTask.WhenAll で待機）
        // ---------------------------------------------------------------------

        public static Sequence JoinLoad(this Sequence seq, string storeKey, string addressableName, float offset = 0f, bool persisted = false)
            => seq.Join(new LoadSceneAction(seq.Service, seq.Store, storeKey, addressableName, persisted), offset);

        public static Sequence JoinActivate(this Sequence seq, string keyOrStoreKey, float offset = 0f)
            => seq.Join(new ActivateSceneAction(seq.Service, seq.Store, keyOrStoreKey), offset);

        public static Sequence JoinUnload(this Sequence seq, string keyOrStoreKey, float offset = 0f, bool disposeResource = false, bool removeFromStore = true)
            => seq.Join(new UnloadSceneAction(seq.Service, seq.Store, keyOrStoreKey, disposeResource, removeFromStore), offset);

        public static Sequence JoinWaitStored(this Sequence seq, string storeKey, float offset = 0f, float timeoutSeconds = -1f)
            => seq.Join(new WaitForStoredSceneAction(seq.Store, storeKey, timeoutSeconds), offset);

        // ---------------------------------------------------------------------
        // Store Actions (Append/Join) : Play中の任意タイミングで Store に登録
        // ---------------------------------------------------------------------

        /// <summary>
        /// Play中のそのタイミングで「現在アクティブなシーン」を Store に保存（Duration=0）
        /// </summary>
        public static Sequence AppendStoreActive(this Sequence seq, string storeKey)
            => seq.Append(new StoreActiveSceneAction(seq.Service, seq.Store, storeKey));

        public static Sequence JoinStoreActive(this Sequence seq, string storeKey, float offset = 0f)
            => seq.Join(new StoreActiveSceneAction(seq.Service, seq.Store, storeKey), offset);

        /// <summary>
        /// Play中のそのタイミングで「Registry から探したシーン」を Store に保存（Duration=0）
        /// </summary>
        public static Sequence AppendStoreFromRegistry(this Sequence seq, string storeKey, string keyOrName)
            => seq.Append(new StoreSceneFromRegistryAction(seq.Service, seq.Store, storeKey, keyOrName));

        public static Sequence JoinStoreFromRegistry(this Sequence seq, string storeKey, string keyOrName, float offset = 0f)
            => seq.Join(new StoreSceneFromRegistryAction(seq.Service, seq.Store, storeKey, keyOrName), offset);
    }
}
