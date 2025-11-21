using System.Threading.Tasks;

namespace HighElixir.StateMachine.Thead
{
    /// <summary>
    /// 非同期ステートの基底クラス
    /// </summary>
    public abstract class StateAsync<TCont> : State<TCont>
    {
        public virtual Task EnterAsync()
        {
            return Task.CompletedTask;
        }
        public virtual Task UpdateAsync()
        {
            return Task.CompletedTask;
        }
        public virtual Task ExitAsync()
        {
            return Task.CompletedTask;
        }
    }

    public sealed class IdleAsync<TCont> : StateAsync<TCont>
    {
        public static readonly IdleAsync<TCont> Instance = new IdleAsync<TCont>();
    }
}