
using Cysharp.Threading.Tasks;
using HighElixir.Implements.Observables;
using HighElixir.StateMachines;
using HighElixir.StateMachines.Thead.Blocks;
using HighElixir.Timers;
using HighElixir.Timers.Unity;
using System;
using UnityEngine;

public class Test : MonoBehaviour
{
    public enum Event
    {
        Loading,
        Loaded,
        Awake,
        End,
        Reset
    }
    public enum State
    {
        Blank,
        Load,
        Title,
        GamePlay,
        Exit
    }
    [SerializeField] private bool _start = false;
    private StateMachine<Test, Event, State> _fms;
    private TimerTicket _ticket;

    private void Awake()
    {
        var op = new StateMachineOption<Test, Event, State>();
        _fms = new(this, op);

        _fms.RegisterTransition(State.Blank, Event.Loading, State.Load);
        _fms.RegisterTransition(State.Load, Event.Loaded, State.Title);
        _fms.RegisterTransition(State.Title, Event.Awake, State.GamePlay);
        _fms.RegisterTransition(State.GamePlay, Event.End, State.Exit);

        _fms.RegisterAnyTransition(Event.Reset, State.Blank);

        GlobalTimer.Update.CountDownRegister(15, out _ticket)
            .Where(id => TimerEventRegistry.HasAny(id, TimeEventType.Finished))
            .Subscribe(_ =>
            {
                var unused = _fms.Send(Event.End);
            });

        var block = _fms
             .AttachBox()
                 .CreateBox()
                 .SendBlock(Event.Loading)
                 .DelayBlock(TimeSpan.FromSeconds(5))
                 .SendBlock(Event.Loaded)
             .MoveToRoot(true)
             .WaitUntil(() => _start == true)
             .SendBlock(Event.Awake)
                 .CreateBox()
                 .CustomActionBlock(() => GlobalTimer.Update.Send(_ticket, TimeOperation.Start));

        _ = _fms.Awake(State.Blank);
        block.Operate().AsUniTask().Forget();
    }

    private void Update()
    {
        _ = _fms.Update(Time.deltaTime);
    }
}