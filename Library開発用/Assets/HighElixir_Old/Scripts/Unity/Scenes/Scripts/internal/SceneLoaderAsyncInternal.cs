using HighElixir.Unity.Addressable.SceneManagement.Helpers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace HighElixir.Unity.Addressable.SceneManagement.Internal
{
    internal static class SceneLoaderAsyncInternal
    {
        // autoActivateがfalseの場合、autoUnload, notifyは無視される
        internal static async Task<SceneInstance> SceneLoaderAsync(string key, FromSceneContainer container, bool autoActivate, bool autoUnload, bool notify, float currentReport = 0f, float maxReport = 1f, CancellationToken token = default, IProgress<float> progress = null)
        {
            var scene = SceneManager.GetActiveScene();
            var inst = await GetProgress(Addressables.LoadSceneAsync(key, LoadSceneMode.Additive), currentReport, maxReport, token, progress);
            SceneStack.RegisterScene(inst, key);
            if (token.IsCancellationRequested)
                throw new TaskCanceledException();
            var flg = SceneStack.TryGetCurrentSceneInstance(out var from);
            SceneStack.Push(inst);
            if (!autoActivate)
                return inst;
            await inst.ActivateAsync();
            if (autoUnload)
            {
                if (flg)
                    await SceneManageHelper.UnloadSceneAsync(from);
                else
                    // Stackに登録されていないため、現在のアクティブシーンをアンロードする
                    await SceneManager.UnloadSceneAsync(scene);
            }
            if (notify)
                UnityThread.Post(() => SearchAndNotify(inst.Scene, container));
            return inst;
        }
        private static async Task<SceneInstance> GetProgress(AsyncOperationHandle<SceneInstance> operation, float currentReport, float maxReport, CancellationToken token, IProgress<float> progress)
        {
            while (!operation.IsDone)
            {

                if (token.IsCancellationRequested)
                {
                    Addressables.Release(operation);
                    throw new TaskCanceledException();
                }
                progress?.Report(currentReport + (operation.PercentComplete * (maxReport - currentReport)));
                await Task.Yield();
            }
            progress?.Report(maxReport);
            return await operation.Task;
        }

        private static void SearchAndNotify(Scene scene, FromSceneContainer container)
        {
            foreach (var item in scene.GetRootGameObjects().Where(go => go.activeInHierarchy))
            {
                if (item.TryGetComponent<ISceneReceiver>(out var receiver))
                    receiver.Receive(container);
            }
        }

    }
}