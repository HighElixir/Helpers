using HighElixir.Unity.Addressable.SceneManagement.Internal;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace HighElixir.Unity.Addressable.SceneManagement
{
    public readonly struct FromSceneContainer
    {
        public readonly string[] Args;
        public readonly AssetReference SceneReference;

        public FromSceneContainer(AssetReference sceneReference, params string[] args)
        {
            this.SceneReference = sceneReference;
            this.Args = args;
        }

        public bool TryGetInst(out SceneInstance instance)
        {
            return SceneStack.TryGetScene(SceneReference, out instance);
        }
    }
}