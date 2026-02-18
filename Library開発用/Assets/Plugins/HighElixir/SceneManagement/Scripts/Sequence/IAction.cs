using Cysharp.Threading.Tasks;
using System.Threading;

namespace HighElixir.HESceneManager.DirectionSequence
{
    public interface IAction
    {
        float Time { get; set; }
        float Duration { get; set; }
        UniTask DO(CancellationToken token);
    }
}