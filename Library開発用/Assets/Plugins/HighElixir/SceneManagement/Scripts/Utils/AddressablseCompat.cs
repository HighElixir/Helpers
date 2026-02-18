using System;
using System.Collections.Generic;
using System.Linq;

namespace HighElixir.HESceneManager.Utils
{
    /// <summary>
    /// Addressable名とScene名の差異を吸収するための互換レイヤー。
    /// - addressableName <-> sceneName を同一IDに紐付ける
    /// - 既に別IDになっている場合は統合（merge）する
    /// </summary>
    public static class AddressablesCompat
    {
        // name -> id
        private static readonly Dictionary<string, string> _addressableToID = new();
        private static readonly Dictionary<string, string> _sceneNameToID = new();

        // id -> (addressableName, sceneName)
        private static readonly Dictionary<string, Names> _idToNames = new();

        // Unityは基本メインスレッドだけど、念のため同期したい時の保険
        private static readonly object _lock = new();

        private readonly struct Names
        {
            public readonly string Addressable;
            public readonly string SceneName;

            public Names(string addressable, string sceneName)
            {
                Addressable = addressable ?? "";
                SceneName = sceneName ?? "";
            }

            public Names WithAddressable(string addressable)
                => new Names(string.IsNullOrEmpty(addressable) ? Addressable : addressable, SceneName);

            public Names WithSceneName(string sceneName)
                => new Names(Addressable, string.IsNullOrEmpty(sceneName) ? SceneName : sceneName);
        }

        /// <summary>
        /// 明示的に「このAddressable名とこのScene名は同じだ」と紐付ける。
        /// 既に別IDになっていたら統合（merge）する。
        /// </summary>
        public static string Link(string addressableName, string sceneName)
        {
            if (string.IsNullOrEmpty(addressableName)) throw new ArgumentException("addressableName is null/empty");
            if (string.IsNullOrEmpty(sceneName)) throw new ArgumentException("sceneName is null/empty");

            lock (_lock)
            {
                var idA = GetOrCreateIdFromAddressable_NoLock(addressableName);
                var idS = GetOrCreateIdFromSceneName_NoLock(sceneName);

                if (idA == idS)
                {
                    // 同じIDなら、欠けてる側を埋めるだけ
                    EnsureNames_NoLock(idA, addressableName, sceneName);
                    return idA;
                }

                // 別IDなら統合
                return Merge_NoLock(keepId: idA, removeId: idS, addressableName, sceneName);
            }
        }

        /// <summary>
        /// Addressable名からIDを取得。未登録なら仮登録してID発行。
        /// </summary>
        public static string GetIDFromAddressable(string addressableName)
        {
            if (string.IsNullOrEmpty(addressableName)) return "";
            lock (_lock) return GetOrCreateIdFromAddressable_NoLock(addressableName);
        }

        /// <summary>
        /// Scene名からIDを取得。未登録なら仮登録してID発行。
        /// </summary>
        public static string GetIDFromSceneName(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return "";
            lock (_lock) return GetOrCreateIdFromSceneName_NoLock(sceneName);
        }

        /// <summary>
        /// Addressable名 -> Scene名。未リンクなら "" を返す。
        /// </summary>
        public static string AddressableToSceneName(string addressableName)
        {
            if (string.IsNullOrEmpty(addressableName)) return "";
            lock (_lock)
            {
                var id = GetOrCreateIdFromAddressable_NoLock(addressableName);
                return _idToNames.TryGetValue(id, out var names) ? names.SceneName : "";
            }
        }

        /// <summary>
        /// Scene名 -> Addressable名。未リンクなら "" を返す。
        /// </summary>
        public static string SceneNameToAddressable(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return "";
            lock (_lock)
            {
                var id = GetOrCreateIdFromSceneName_NoLock(sceneName);
                return _idToNames.TryGetValue(id, out var names) ? names.Addressable : "";
            }
        }

        /// <summary>
        /// IDから両方の名前を取得（デバッグや同期確認用）
        /// </summary>
        public static bool TryGetNamesFromID(string id, out string addressableName, out string sceneName)
        {
            addressableName = "";
            sceneName = "";
            if (string.IsNullOrEmpty(id)) return false;

            lock (_lock)
            {
                if (!_idToNames.TryGetValue(id, out var names)) return false;
                addressableName = names.Addressable;
                sceneName = names.SceneName;
                return true;
            }
        }

        /// <summary>
        /// テスト用: 全消去
        /// </summary>
        public static void ClearAll()
        {
            lock (_lock)
            {
                _addressableToID.Clear();
                _sceneNameToID.Clear();
                _idToNames.Clear();
            }
        }
        public static bool TryGetIDFromAddressable(string addressableName, out string id)
        {
            id = "";
            if (string.IsNullOrEmpty(addressableName)) return false;
            lock (_lock) return _addressableToID.TryGetValue(addressableName, out id);
        }

        public static bool TryGetIDFromSceneName(string sceneName, out string id)
        {
            id = "";
            if (string.IsNullOrEmpty(sceneName)) return false;
            lock (_lock) return _sceneNameToID.TryGetValue(sceneName, out id);
        }


        // -------------------------
        // Internal (NoLock)
        // -------------------------

        private static string GetOrCreateIdFromAddressable_NoLock(string addressableName)
        {
            if (_addressableToID.TryGetValue(addressableName, out var id))
                return id;

            id = GenerateID();
            _addressableToID[addressableName] = id;
            _idToNames[id] = new Names(addressableName, "");
            return id;
        }

        private static string GetOrCreateIdFromSceneName_NoLock(string sceneName)
        {
            if (_sceneNameToID.TryGetValue(sceneName, out var id))
                return id;

            id = GenerateID();
            _sceneNameToID[sceneName] = id;
            _idToNames[id] = new Names("", sceneName);
            return id;
        }

        private static void EnsureNames_NoLock(string id, string addressableName, string sceneName)
        {
            if (!_idToNames.TryGetValue(id, out var names))
                names = new Names("", "");

            names = names.WithAddressable(addressableName).WithSceneName(sceneName);
            _idToNames[id] = names;

            // 逆引き辞書側も、必ずこのidを向くように揃える
            if (!string.IsNullOrEmpty(addressableName)) _addressableToID[addressableName] = id;
            if (!string.IsNullOrEmpty(sceneName)) _sceneNameToID[sceneName] = id;
        }

        /// <summary>
        /// removeId を keepId に統合する。
        /// keepId/removeId のどちらにも複数名が紐づいている可能性があるので、
        /// 辞書を走査して removeId を参照しているキーを全部 keepId に付け替える。
        /// </summary>
        private static string Merge_NoLock(string keepId, string removeId, string addressableName, string sceneName)
        {
            // keep側の名前をベースに、remove側で埋められるものがあれば埋める
            var keepNames = _idToNames.TryGetValue(keepId, out var kn) ? kn : new Names("", "");
            var removeNames = _idToNames.TryGetValue(removeId, out var rn) ? rn : new Names("", "");

            // 明示引数（Linkの呼び出し）を最優先で埋める
            keepNames = keepNames
                .WithAddressable(addressableName)
                .WithSceneName(sceneName)
                .WithAddressable(removeNames.Addressable)
                .WithSceneName(removeNames.SceneName);

            _idToNames[keepId] = keepNames;
            _idToNames.Remove(removeId);

            // addressableToID の付け替え
            // removeId を指している全キーを keepId に変更
            var addressableKeysToMove = _addressableToID
                .Where(kv => kv.Value == removeId)
                .Select(kv => kv.Key)
                .ToArray();
            foreach (var key in addressableKeysToMove)
                _addressableToID[key] = keepId;

            // sceneNameToID の付け替え
            var sceneKeysToMove = _sceneNameToID
                .Where(kv => kv.Value == removeId)
                .Select(kv => kv.Key)
                .ToArray();
            foreach (var key in sceneKeysToMove)
                _sceneNameToID[key] = keepId;

            // 念のため、今回の引数も確実に keepId へ
            if (!string.IsNullOrEmpty(addressableName)) _addressableToID[addressableName] = keepId;
            if (!string.IsNullOrEmpty(sceneName)) _sceneNameToID[sceneName] = keepId;

            return keepId;
        }

        private static string GenerateID() => Guid.NewGuid().ToString();
    }
}
