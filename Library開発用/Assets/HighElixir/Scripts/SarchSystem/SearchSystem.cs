using System;
using System.Collections.Generic;
using UnityEngine;
namespace HighElixir.SearchSystems
{
    [Serializable]
    public sealed class SearchSystem<TCont, TTarget> : IDisposable
    {
        [Serializable]
        public sealed class Profile : IDisposable
        {
            public string SystemName;

            [SerializeReference]
            public List<ISearchComponent> Components = new();

            public List<TTarget> Execute(TCont cont, List<TTarget> targets)
            {
                var results = new List<TTarget>(targets);
                foreach (var component in Components)
                {
                    component.ExecuteSearch(cont, ref results);
                }
                return results;
            }

            public void Dispose()
            {
                if (Components != null)
                {
                    foreach (var component in Components)
                    {
                        component.Dispose();
                    }
                    Components.Clear();
                    Components = null;
                }
            }
        }
        public interface ISearchComponent : IDisposable
        {
            void Initialize();
            void ExecuteSearch(TCont cont, ref List<TTarget> targets);
        }

        private TCont _context;
        private Dictionary<string, Profile> _profiles = new();

        // 状態
        private bool _awakened = false;

        public bool IsAwakened => _awakened;
        /// <summary>
        /// コンテキストの初期化
        /// </summary>
        /// <param name="cont"></param>
        public void Awake(TCont cont)
        {
            _context = cont;
            _awakened = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="targets"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public List<TTarget> ExecuteSearch(string profile, List<TTarget> targets)
        {
            if (!_awakened) throw new InvalidOperationException("SearchSystem must be awakened before executing search.");
            if (!_profiles.ContainsKey(profile)) throw new ArgumentException($"Profile '{profile}' does not exist.");
            var selectedProfile = _profiles[profile];
            return selectedProfile.Execute(_context, targets);
        }

        #region Profile Management
        public void AddProfile(string systemName = "", params ISearchComponent[] components)
        {
            if (_awakened) throw new InvalidOperationException("Cannot add profiles after the SearchSystem has been awakened.");
            var profile = new Profile
            {
                SystemName = string.IsNullOrEmpty(systemName) ? "New Profile" : systemName,
                Components = new List<ISearchComponent>(components)
            };
            _profiles.Add(profile.SystemName, profile);
            RenameProfiles();
        }

        public void RemoveProfile(string systemName)
        {
            if (_awakened) throw new InvalidOperationException("Cannot remove profiles after the SearchSystem has been awakened.");
            if (_profiles.ContainsKey(systemName))
            {
                _profiles[systemName].Dispose();
                _profiles.Remove(systemName);
                RenameProfiles();
            }
        }

        public void ClearProfiles()
        {
            if (_awakened) throw new InvalidOperationException("Cannot clear profiles after the SearchSystem has been awakened.");
            foreach (var profile in _profiles)
            {
                profile.Value.Dispose();
            }
            _profiles.Clear();
        }

        public bool ContainsProfile(string systemName)
        {
            return _profiles.ContainsKey(systemName);
        }

        public bool TryGetProfile(string systemName, out Profile profile)
        {
            return _profiles.TryGetValue(systemName, out profile);
        }

        internal List<Profile> GetProfiles()
        {
            return new List<Profile>(_profiles.Values);
        }

        #region コンポーネント (起動後も操作可能)
        public void AddComponentToProfile(string systemName, params ISearchComponent[] components)
        {
            if (_profiles.ContainsKey(systemName))
            {
                foreach (var component in components)
                {
                    component.Initialize();
                    _profiles[systemName].Components.Add(component);
                }
            }
        }

        public void RemoveComponentFromProfile(string systemName, ISearchComponent component)
        {
            if (_profiles.ContainsKey(systemName))
            {
                if (_profiles[systemName].Components.Contains(component))
                {
                    _profiles[systemName].Components.Remove(component);
                    component.Dispose();
                }
            }
        }

        public bool ContainsComponentInProfile(string systemName, ISearchComponent component)
        {
            if (_profiles.ContainsKey(systemName))
            {
                return _profiles[systemName].Components.Contains(component);
            }
            return false;
        }
        public void ClearComponentsFromProfile(string systemName)
        {
            if (_profiles.ContainsKey(systemName))
            {
                foreach (var component in _profiles[systemName].Components)
                {
                    component.Dispose();
                }
                _profiles[systemName].Components.Clear();
            }
        }
        #endregion

        private void RenameProfiles()
        {
            var names = new Dictionary<string, string>();
            foreach (var profile in _profiles)
            {
                names.Add(profile.Key, profile.Value.SystemName);
            }
            TextFilters.AutoRename(ref names);

            foreach (var p in names.Keys)
            {
                _profiles[p].SystemName = names[p];
            }
        }
        #endregion

        #region Disposable Pattern
        private bool _isDisposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    foreach (var profile in _profiles)
                    {
                        foreach (var component in profile.Value.Components)
                        {
                            component.Dispose();
                        }
                    }
                    _profiles.Clear();
                }
                // Dispose unmanaged resources here if any
                _isDisposed = true;
            }
        }

        ~SearchSystem()
        {
            Dispose(false);
        }
        #endregion
    }
}