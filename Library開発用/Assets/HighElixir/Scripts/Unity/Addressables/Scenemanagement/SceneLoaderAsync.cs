using HighElixir.Unity.Addressable.SceneManagement.Helpers;
using HighElixir.Unity.Addressable.SceneManagement.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace HighElixir.Unity.Addressable.SceneManagement
{
    public static class SceneLoaderAsync
    {
        public static async Task<SceneInstance> LoadSceneWithManagerAsync(string guid, bool notify = true, CancellationToken token = default, IProgress<float> progress = null, params string[] args)
        {
            if (SceneManageHelper.TryCreateReference(guid, out var reference))
                return await LoadSceneWithManagerAsync(reference, notify, token, progress);
            Debug.LogError($"[SceneLoaderAsync] Invalid GUID: {guid}");
            return default;
        }

        public static async Task<SceneInstance> LoadSceneAsync(string guid, bool notify = true, bool autoUnload = true, CancellationToken token = default, IProgress<float> progress = null, params string[] args)
        {
            if (SceneManageHelper.TryCreateReference(guid, out var reference))
                return await LoadSceneAsync(reference, notify, autoUnload, token, progress);
            Debug.LogError($"[SceneLoaderAsync] Invalid GUID: {guid}");

            return default;
        }

        public static async Task<SceneInstance> LoadSceneWithManagerAsync(AssetReference reference, bool notify = true, CancellationToken token = default, IProgress<float> progress = null, params string[] args)
        {
            if (ManagerSceneHolder.TryGetManagerScene(out var m))
            {
                await SceneManageHelper.UnloadCurrentSceneAsync();
                progress?.Report(0.1f);
                await m.ActivateAsync();
                progress?.Report(0.1f);
            }
            var cont = new FromSceneContainer(reference, args);
            return await SceneLoaderAsyncInternal.SceneLoaderAsync(reference, cont, notify, false, 0.2f, 1.2f, token, progress);
        }
        public static async Task<SceneInstance> LoadSceneAsync(AssetReference reference, bool notify = true, bool autoUnload = true, CancellationToken token = default, IProgress<float> progress = null, params string[] args)
        {
            var cont = new FromSceneContainer(reference, args);
            return await SceneLoaderAsyncInternal.SceneLoaderAsync(
                reference,
                cont,
                autoUnload,
                notify,
                0f,
                1f,
                token,
                progress
            );
        }
    }
}