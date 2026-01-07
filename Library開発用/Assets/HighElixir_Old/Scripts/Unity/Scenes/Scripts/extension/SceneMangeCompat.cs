using HighElixir.Unity.Addressable.SceneManagement.Helpers;
using System.Threading.Tasks;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace HighElixir.Unity.Addressable.SceneManagement
{
    public static class SceneManageCompat
    {
        public static async Task ActivateAndUnloadSceneAsync(this SceneInstance sceneInstance)
        {
            await SceneManageHelper.UnloadCurrentSceneAsync();
            sceneInstance.ActivateAsync();
        }
    }
}