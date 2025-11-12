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
        private static bool _loading = false;
        private static SceneInstance _managerScene;
        private static AssetReference _assetReference;

        public static SceneInstance ManagerScene => _managerScene;
        public static AssetReference AssetReference => _assetReference;
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
        public static async Task SetManageScene(string guid, CancellationToken token = default, IProgress<float> progress = null, bool isActiveReturn = true)
        {
            if(SceneManageHelper.TryCreateReference(guid, out var reference))
            {
                await SetManageScene(reference, token, progress);
            }
        }
        public static async Task SetManageScene(AssetReference reference, CancellationToken token = default, IProgress<float> progress = null, bool isActiveReturn = true)
        {
            if (_loading) return;
            _loading = true;
            try
            {
                _assetReference = reference;
                _loaded = false;
                Func<Task> ret = null;
                if (isActiveReturn)
                {
                    if (SceneStack.TryGetCurrentSceneInstance(out var sceneInstance))
                    {
                        ret = async () => await sceneInstance.ActivateAsync();
                    }
                    else
                    {
                        ret = async () =>
                        {
                            SceneManager.SetActiveScene(SceneManager.GetActiveScene());
                            await Task.FromResult(0);
                        };
                    }
                }
                _managerScene = await SceneLoaderAsync.LoadSceneAsync(reference, true, false, token, progress);
                if (isActiveReturn && ret != null) await ret();
                _loaded = true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _loading = false;
            }
        }
        public static async Task UnloadManagerSceneAsync()
        {
            if (_loaded)
            {
                await Addressables.UnloadSceneAsync(_managerScene, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects).Task;
                _managerScene = default;
                _assetReference = null;
                _loaded = false;
            }
        }

    }
}