using System.Threading;
using System.Threading.Tasks;

namespace HighElixir.StateMachines.Thead
{
    /// <summary>
    /// 非同期ステートの基底クラス
    /// </summary>
    public abstract class StateAsync<TCont> : State<TCont>
    {
        public virtual Task EnterAsync(CancellationToken token)
        {
            return Task.CompletedTask;
        }
        public virtual Task UpdateAsync(CancellationToken token)
        {
            return Task.CompletedTask;
        }
        public virtual Task ExitAsync(CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }

    public sealed class IdleAsync<TCont> : StateAsync<TCont>
    {
        public static readonly IdleAsync<TCont> Instance = new IdleAsync<TCont>();
    }
}