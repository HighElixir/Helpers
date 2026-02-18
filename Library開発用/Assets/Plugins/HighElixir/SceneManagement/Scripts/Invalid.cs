using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace HighElixir.HESceneManager
{
    public static class Invalids
    {
        public static readonly Scene InvalidScene;
        public static readonly SceneInstance InvalidSceneInstance;

        public static bool IsInvalid(this Scene scene)
            => scene.Equals(InvalidScene);

        public static bool IsInvalid(this SceneInstance sceneInstance)
            => sceneInstance.Scene.IsInvalid();

        static Invalids()
        {
            InvalidScene = new() { name = "Invalid" };
            InvalidSceneInstance = new();
            typeof(SceneInstance).GetProperty("Scene").SetValue(InvalidSceneInstance, InvalidScene);
        }
    }
}