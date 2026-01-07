using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

namespace HighElixir.Unity.UniTasks
{
    public static class UniTaskExtensions
    {
        /// <summary>
        /// タスクをUniTaskに変換してForgetする拡張メソッド
        /// </summary>
        public static void Forget(this Task task)
        {
            task.AsUniTask().Forget();
        }
    }
}