using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HighElixir.SceneManagement
{
    public static class SceneLoaderAsync
    {
        private static bool _loaded = false;
        private static Scene _managerScene;

        public static Scene TryGetManagerScene()
        {
            if (_loaded) return _managerScene;
            return default;
        }
        public static async Task SetManageScene(int buildIdx, CancellationToken token = default, IProgress<float> progress = null, bool isActiveReturn = true)
        {
            Scene from = default;
            if (isActiveReturn) from = SceneManager.GetActiveScene();
            _managerScene = await LoadByBuildIdx(buildIdx, token, progress);
            if (isActiveReturn) SceneManager.SetActiveScene(from);
            _loaded = true;
        }
        public static async Task<Scene> LoadByBuildIdx(int buildIdx, CancellationToken token = default, IProgress<float> progress = null)
        {
            return await SceneLoaderAsyncInternal.SceneLoadeAsync(async () =>
            {
                var op = SceneManager.LoadSceneAsync(buildIdx, LoadSceneMode.Additive);
                await SceneLoaderAsyncInternal.GetProgress(op, progress);
                return buildIdx;
            }, token, progress);
        }

        public static async Task LoadSceneWithManageAsyncByBuildIdx(int buildIdx, CancellationToken token = default, IProgress<float> progress = null)
        {
            if (_loaded)
            {
                var s = SceneManager.GetActiveScene();
                SceneManager.SetActiveScene(_managerScene);
                _ = SceneManager.UnloadSceneAsync(s);
            }
            await LoadByBuildIdx(buildIdx, token, progress);
        }
    }
}