using UnityEngine.SceneManagement;

namespace HighElixir.HESceneManager.Utils
{
    public static class SceneServiceExtension
    {
        /// <summary>
        /// 現在アクティブなシーンを SceneData として取得する。
        /// 可能なら Registry 内の既存 SceneData を返し、無ければ生成して登録する。
        /// </summary>
        public static SceneData GetOrRegisterActiveSceneData(this HESceneService hE)
        {
            var active = SceneManager.GetActiveScene();

            if (hE.Registry.TryGetScene(active, out var existing))
                return existing;

            var created = new SceneData(active, isPersisted: false);
            hE.Registry.RegisterScene(created);
            hE.Registry.SetCurrentScene(created);
            return created;
        }

        /// <summary>
        /// 互換維持用：旧API名。Registryを触れないので、単にSceneDataを生成するだけ。
        /// 注意：この戻り値を Unregister のキーに使うと消せない場合がある。
        /// </summary>
        public static SceneData GetActiveSceneAndPacking()
        {
            var active = SceneManager.GetActiveScene();
            return new SceneData(active, false);
        }

        public static SceneData[] GetAllLoadedScenes()
        {
            int sceneCount = SceneManager.sceneCount;
            SceneData[] scenes = new SceneData[sceneCount];
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                scenes[i] = new SceneData(scene, false);
            }
            return scenes;
        }

        /// <summary>
        /// シーンを非同期に破棄するためのDisposerを作成します
        /// </summary>
        public static SceneScopeHandle CreateAsyncDisposable(
            this HESceneService hE,
            SceneData sceneData,
            SceneScopeHandle.HandleOnDispose handleOnDispose = SceneScopeHandle.HandleOnDispose.Unload)
        {
            // sceneDataが未登録の可能性があるので、ここで登録しておくと安全
            if (sceneData != null && !sceneData.IsInvalid())
                hE.Registry.RegisterScene(sceneData);

            return new SceneScopeHandle(sceneData, hE, handleOnDispose);
        }

        public static SceneScopeHandle CreateAsyncDisposableFromActive(
            this HESceneService hE,
            SceneScopeHandle.HandleOnDispose handleOnDispose = SceneScopeHandle.HandleOnDispose.Unload)
        {
            // ★重要：新規生成ではなく、Registryの既存を優先
            var activeSceneData = hE.GetOrRegisterActiveSceneData();
            return new SceneScopeHandle(activeSceneData, hE, handleOnDispose);
        }

        public static void RegisterLoadedScenes(this HESceneService hE)
        {
            // まとめて登録。RegisterSceneがCompatIDで吸収する前提。
            hE.Registry.RegisterScenes(GetAllLoadedScenes());
        }

        /// <summary>
        /// もし「このシーン名はこのアドレスだ」と事前に分かっているなら、
        /// ここで Link してから登録すると、以後の検索が強くなる。
        /// </summary>
        public static void LinkAndRegisterLoadedScenes(
            this HESceneService hE,
            params (string addressableName, string sceneName)[] links)
        {
            if (links != null)
            {
                foreach (var (ad, sn) in links)
                {
                    if (!string.IsNullOrEmpty(ad) && !string.IsNullOrEmpty(sn))
                        AddressablesCompat.Link(ad, sn);
                }
            }

            hE.RegisterLoadedScenes();
        }
    }
}
