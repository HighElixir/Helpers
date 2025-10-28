using System.Collections.Generic;
using UnityEngine;

namespace HighElixir.SearchSystems
{
    /// <summary>
    /// SearchSystemをUnity上で使いやすくするための抽象基底クラス。
    /// エディタ上でProfileを設定・編集し、実行時に自動同期される。
    /// </summary>
    public abstract class SearchMono<TCont, TTarget> : MonoBehaviour
    {
        [SerializeField, Tooltip("この検索モノに関連付けられた検索プロファイル群")]
        private List<SearchSystem<TCont, TTarget>.Profile> _profiles = new();

        private readonly SearchSystem<TCont, TTarget> _searchSystem = new();

        private bool _initialized;

        /// <summary>
        /// 登録されている全プロファイルを取得
        /// </summary>
        public IReadOnlyList<SearchSystem<TCont, TTarget>.Profile> Profiles => _profiles.AsReadOnly();

        /// <summary>
        /// 検索システムを初期化し、Profilesを同期
        /// </summary>
        protected void InitializeProfiles()
        {
            if (_initialized) return;

            _searchSystem.ClearProfiles();
            foreach (var profile in _profiles)
            {
                if (profile == null) continue;
                _searchSystem.AddProfile(profile.SystemName, profile.Components.ToArray());
            }

            _initialized = true;
        }

        /// <summary>
        /// プロファイル名を指定して検索を実行。
        /// </summary>
        public List<TTarget> ExecuteSearch(string profileName, List<TTarget> targets, TCont context)
        {
            _searchSystem.Awake(context);
            return _searchSystem.ExecuteSearch(profileName, targets);
        }


        /// <summary>
        /// Awake時にSearchSystemを初期化
        /// </summary>
        protected virtual void Awake()
        {
            InitializeProfiles();
        }
        /// <summary>
        /// 終了時のクリーンアップ
        /// </summary>
        protected virtual void OnDestroy()
        {
            _searchSystem.Dispose();
        }

#if UNITY_EDITOR
        /// <summary>
        /// エディタ上で値が変わった時に自動同期（Editorのみ）
        /// </summary>
        private void OnValidate()
        {
            // プロファイル名の自動整理
            var names = new Dictionary<string, string>();
            foreach (var p in _profiles)
            {
                if (p != null)
                    names.Add(p.SystemName, p.SystemName);
            }

            TextFilters.AutoRename(ref names);

            int i = 0;
            foreach (var item in names.Values)
            {
                if (_profiles[i] != null)
                    _profiles[i].SystemName = item;
                i++;
            }

            // エディタ上でもSearchSystemに反映
            if (Application.isPlaying == false)
            {
                _searchSystem.ClearProfiles();
                foreach (var profile in _profiles)
                {
                    if (profile == null) continue;
                    _searchSystem.AddProfile(profile.SystemName, profile.Components.ToArray());
                }
            }
        }
#endif
    }
}
