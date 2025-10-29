using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace HighElixir.Unity.SceneManagement
{
    public static class ManagerSceneHolder
    {
        private static bool _loaded = false;
        private static Scene _managerScene;

        public static bool TryGetManagerScene(out Scene managerScene)
        {
            if (_loaded)
            {
                managerScene = _managerScene;
                return true;
            }
            managerScene = default;
            return false;
        }
        public static async Task SetManageScene(int buildIdx, CancellationToken token = default, IProgress<float> progress = null, bool isActiveReturn = true)
        {
            _loaded = false;
            Scene from = default;
            if (isActiveReturn) from = SceneManager.GetActiveScene();
            _managerScene = await SceneLoaderAsync.LoadByBuildIdxAsync(buildIdx, token, progress);
            if (isActiveReturn && from != default) SceneManager.SetActiveScene(from);
            _loaded = true;
        }
    }
}