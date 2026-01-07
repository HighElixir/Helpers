using HighElixir.Unity.Addressable.SceneManagement.Internal;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace HighElixir.Unity.Addressable.SceneManagement
{
    public readonly struct FromSceneContainer
    {
        public readonly string[] Args;
        public readonly SceneInstance Instance;

        public FromSceneContainer(SceneInstance scene, params string[] args)
        {
            this.Instance = scene;
            this.Args = args;
        }
    }
}