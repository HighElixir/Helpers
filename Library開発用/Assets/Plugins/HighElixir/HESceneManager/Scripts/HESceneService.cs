using Cysharp.Threading.Tasks;
using HighElixir.HESceneManager.Utils;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace HighElixir.HESceneManager
{
    public sealed class HESceneService
    {
        private readonly SceneRegistry _registry = new();
        public SceneRegistry Registry => _registry;

        public HESceneService()
        {
            _registry.SetCurrentScene(SceneServiceExtension.GetActiveSceneAndPacking());
        }

        public async UniTask<SceneData> LoadSceneAsync(string addressableName)
        {
            // ここは「検索で新規ID発行しない」TryGetScene実装になってる想定
            if (_registry.TryGetScene(addressableName, out var sceneData))
                return sceneData;

            var handle = Addressables.LoadSceneAsync(
                addressableName,
                UnityEngine.SceneManagement.LoadSceneMode.Additive);

            var instance = await handle;

            // ★重要：addressableName を渡す（Linkが成立してCompatIDが統一される）
            var newSceneData = new SceneData(instance, isPersisted: false, addressableName: addressableName);

            _registry.RegisterScene(newSceneData);
            return newSceneData;
        }

        public async UniTask ActivateAsync(SceneData sceneData)
        {
            if (sceneData == null || sceneData.IsInvalid()) return;

            if (await sceneData.ActivateAsync())
                _registry.SetCurrentScene(sceneData);
        }

        public async UniTask UnloadAsync(SceneData sceneData, bool disposeResource = false)
        {
            if (sceneData == null || sceneData.IsInvalid()) return;

            await sceneData.UnloadAsync();
            _registry.UnregisterScene(sceneData);

            if (disposeResource)
            {
                // ★メインスレッドで await するのが安全
                await Resources.UnloadUnusedAssets().ToUniTask();
            }
        }

        public async UniTask UnloadAllUnnecessaryScenesAsync(bool disposeResource = false, bool ignorePersisted = false)
        {
            // ★列挙中に辞書が変わるのを防ぐためスナップショット化
            var loadedScenes = _registry.GetAllRegisteredScenes().ToArray();

            foreach (var sceneData in loadedScenes)
            {
                if (sceneData == null || sceneData.IsInvalid()) continue;
                if (!ignorePersisted && sceneData.IsPersisted) continue;

                if (sceneData.ShouldBeUnloaded)
                    await UnloadAsync(sceneData, disposeResource: false);
            }

            if (disposeResource)
            {
                await Resources.UnloadUnusedAssets().ToUniTask();
            }
        }
    }
}
