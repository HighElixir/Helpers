using System.Threading.Tasks;
using UnityEngine;

namespace HighElixir.Unity.Tasks
{
    public static class AsyncOparationConverter
    {
        public static Task AsTask(this AsyncOperation op)
        {
            var tcs = new TaskCompletionSource<bool>();
            op.completed += _ => tcs.SetResult(true);
            return tcs.Task;
        }
    }
}