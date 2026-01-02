using System.Threading.Tasks;

namespace HighElixir.StateMachines.Thead.Blocks
{
    public static class BlockExtensions
    {
        public static EventBlock<TCont, TEvt, TState> AttachBox<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> machine)
        {
            EventBlock<TCont, TEvt, TState> block = new(machine, _ => Task.CompletedTask);
            return block;
        }

        public static EventBlock<TCont, TEvt, TState> CreateBox<TCont, TEvt, TState>(this EventBlock<TCont, TEvt, TState> block)
        {
            EventBlock<TCont, TEvt, TState> b = new(block, _ => Task.CompletedTask, true);
            return b;
        }

        public static EventBlock<TCont, TEvt, TState> MoveToRoot<TCont, TEvt, TState>(this EventBlock<TCont, TEvt, TState> block, bool bottom = true)
        {
            var root = block.Root;
            return bottom ? root.GetBottom() : root.GetTop();
        }

        public static EventBlock<TCont, TEvt, TState> AttachBottomTo<TCont, TEvt, TState>(this EventBlock<TCont, TEvt, TState> target, EventBlock<TCont, TEvt, TState> block)
        {
            return target.AttachBottom(block);
        }
        public static EventBlock<TCont, TEvt, TState> AttachTopTo<TCont, TEvt, TState>(this EventBlock<TCont, TEvt, TState> target, EventBlock<TCont, TEvt, TState> block)
        {
            return target.AttachTop(block);
        }
    }
}