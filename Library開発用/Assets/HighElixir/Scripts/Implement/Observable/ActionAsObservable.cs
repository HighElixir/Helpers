namespace HighElixir.Implements.Observables
{
    /// <summary>
    /// 戻り値を持たないアクションをObservable化
    /// </summary>
    public class ActionAsObservable : ReactiveProperty<byte>
    {
        public void Invoke() => OnNext();
    }

    /// <summary>
    /// コンテキスト付きアクションをObservable化
    /// </summary>
    public class ActionAsObservable<TCont> : ReactiveProperty<TCont>
    {
        public void SetContext(TCont context) => _value = context;
        public void Invoke() => OnNext();
    }
}
