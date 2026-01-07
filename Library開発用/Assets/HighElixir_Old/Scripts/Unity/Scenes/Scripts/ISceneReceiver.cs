namespace HighElixir.Unity.Addressable.SceneManagement
{
    /// <summary>
    /// MonoBehaviourにアタッチすることで、シーン遷移時に通知することができる
    /// </summary>
    /// <remarks>
    /// シーンルートに置き、遷移時点でアクティブ化されている必要がある
    /// </remarks>
    public interface ISceneReceiver
    {
        void Receive(FromSceneContainer container);
    }
}