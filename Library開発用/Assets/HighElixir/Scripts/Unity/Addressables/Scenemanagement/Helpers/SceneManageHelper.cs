using HighElixir.Unity.Addressable.SceneManagement.Internal;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace HighElixir.Unity.Addressable.SceneManagement.Helpers
{
    public static class SceneManageHelper
    {
        public static async Task UnloadSceneAsync(Scene scene)
        {
            if (SceneStack.TryGetScene(scene, out var inst))
               await UnloadSceneAsync(inst);
        }
        public static async Task UnloadSceneAsync(SceneInstance scene)
        {
            await Addressables.UnloadSceneAsync(scene).Task;
            SceneStack.UnregisterScene(scene);
        }

        public static async Task UnloadCurrentSceneAsync()
        {
            if (SceneStack.TryGetCurrentSceneInstance(out var inst))
            {
                await Addressables.UnloadSceneAsync(inst, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects).Task;
                SceneStack.UnregisterScene(inst);
            }
            else
            {
                await SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
            }
        }

        public static bool TryCreateReference(string guid, out AssetReference assetReference)
        {
            assetReference = null;
            if (string.IsNullOrEmpty(guid)) return false;
            try
            {
                assetReference = new(guid);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}