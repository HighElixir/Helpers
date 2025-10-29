using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HighElixir.Unity.SceneManagement
{
    public static class SceneLoaderAsync
    {
        public static async Task<Scene> LoadByBuildIdxAsync(int buildIdx, CancellationToken token = default, IProgress<float> progress = null)
        {
            return await SceneLoaderAsyncInternal.SceneLoaderAsync(
                SceneManager.LoadSceneAsync(buildIdx, LoadSceneMode.Additive),
                buildIdx,
                token,
                progress
            );
        }

        public static async Task LoadSceneWithManagerAsync(int buildIdx, CancellationToken token = default, IProgress<float> progress = null)
        {
            if (ManagerSceneHolder.TryGetManagerScene(out var m))
            {
                var s = SceneManager.GetActiveScene();
                SceneManager.SetActiveScene(m);
                _ = SceneManager.UnloadSceneAsync(s);
            }
            await LoadByBuildIdxAsync(buildIdx, token, progress);
        }
    }
}