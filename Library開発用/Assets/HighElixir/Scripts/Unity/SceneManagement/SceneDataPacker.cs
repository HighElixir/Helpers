using UnityEngine.SceneManagement;

namespace HighElixir.Unity.SceneManagement
{
    public class SceneDataContainer
    {
        private object _value;
        private Scene _toScene;

        public object Value => _value;
        public Scene Scene => _toScene;
        public SceneDataContainer(Scene scene, object value = null)
        {
            _toScene = scene;
            _value = value;
        }
    }
}