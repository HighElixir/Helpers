using HighElixir.Unity.Addressable.SceneManagement.Helpers;
using HighElixir.Unity.Addressable.SceneManagement.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace HighElixir.Unity.Addressable.SceneManagement
{
    public static class ManagerSceneHolder
    {
        private static bool _loaded = false;
        private static SceneInstance _managerScene;

        public static SceneInstance ManagerScene => _managerScene;

        public static bool TryGetManagerScene(out SceneInstance managerScene)
        {
            if (_loaded)
            {
                managerScene = _managerScene;
                return true;
            }
            managerScene = default;
            return false;
        }
        public static async Task SetManageScene(string key, bool autoActive, CancellationToken token = default, IProgress<float> progress = null, bool isActiveReturn = true)
        {
            _managerScene = await SceneLoaderAsync.LoadSceneAsync(key, autoActive, true, false, token, progress);
        }
        
        public static async Task UnloadManagerSceneAsync()
        {
            if (_loaded)
            {
                await Addressables.UnloadSceneAsync(_managerScene, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects).Task;
                _managerScene = default;
                _loaded = false;
            }
        }

    }
}