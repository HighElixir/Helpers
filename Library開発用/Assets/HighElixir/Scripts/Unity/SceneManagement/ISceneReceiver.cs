namespace HighElixir.Unity.SceneManagement
{
    /// <summary>
    /// MonoBehaviourにアタッチすることで、シーン遷移時に通知することができる
    /// </summary>
    public interface ISceneReceiver
    {
        void Receive(SceneDataContainer container);
    }
}