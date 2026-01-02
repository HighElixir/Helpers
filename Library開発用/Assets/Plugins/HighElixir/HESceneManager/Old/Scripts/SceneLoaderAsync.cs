using HighElixir.Unity.Addressable.SceneManagement.Helpers;
using HighElixir.Unity.Addressable.SceneManagement.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace HighElixir.Unity.Addressable.SceneManagement
{
    public static class SceneLoaderAsync
    {
        public static async Task<SceneInstance> LoadSceneWithManagerAsync(string key, bool autoActive, bool notify = true, CancellationToken token = default, IProgress<float> progress = null, params string[] args)
        {
            if (ManagerSceneHolder.TryGetManagerScene(out var m))
            {
                await SceneManageHelper.UnloadCurrentSceneAsync();
                progress?.Report(0.1f);
                await m.ActivateAsync();
                progress?.Report(0.2f);
            }
            var cont = new FromSceneContainer(m, args);
            return await SceneLoaderAsyncInternal.SceneLoaderAsync(key, cont, autoActive, notify, false, 0.2f, 1.0f, token, progress);
        }

        public static async Task<SceneInstance> LoadSceneAsync(string key, bool autoActive, bool notify = true, bool autoUnload = true, CancellationToken token = default, IProgress<float> progress = null, params string[] args)
        {
            var cont = new FromSceneContainer(SceneStack.CurrentScene, args);
            return await SceneLoaderAsyncInternal.SceneLoaderAsync(
                key,
                cont,
                autoActive,
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