using HighElixir.Implements.Observables;
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
        internal static async Task<SceneInstance> SceneLoaderAsync(AssetReference sceneReference, FromSceneContainer container, bool autoUnload, bool notify, float currentReport = 0f, float maxReport = 1f, CancellationToken token = default, IProgress<float> progress = null)
        {
            var inst = await GetProgress(Addressables.LoadSceneAsync(sceneReference, LoadSceneMode.Additive), currentReport, maxReport, token, progress);
            SceneStack.RegisterScene(inst, sceneReference);
            if (token.IsCancellationRequested)
                throw new TaskCanceledException();
            var from = SceneStack.GetCurrentSceneInstance();
            await inst.ActivateAsync();
            SceneStack.Push(sceneReference);
            if (autoUnload)
            {
                await SceneManageHelper.UnloadSceneAsync(from);
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