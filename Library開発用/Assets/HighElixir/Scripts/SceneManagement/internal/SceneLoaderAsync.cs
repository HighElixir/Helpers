using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HighElixir.SceneManagement
{
    internal static class SceneLoaderAsyncInternal
    {
        internal static async Task<Scene> SceneLoadeAsync(Func<Task<int>> loader, CancellationToken token = default, IProgress<float> progress = null)
        {
            var id = await Task.Run(loader);
            var s = SceneManager.GetSceneByBuildIndex(id);
            SceneManager.SetActiveScene(s);
            FinalizeSceneChange();
            return s;
        }

        internal static async void FinalizeSceneChange()
        {
            await Resources.UnloadUnusedAssets();

        }

        internal static async Task GetProgress(AsyncOperation operation, IProgress<float> progress)
        {
            while (!operation.isDone)
            {
                progress?.Report(operation.progress);
                await Task.Yield();
            }
        }
    }
}