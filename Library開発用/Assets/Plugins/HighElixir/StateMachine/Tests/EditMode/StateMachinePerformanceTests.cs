using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;

namespace HighElixir.StateMachines.Tests.EditMode
{
    public class StateMachinePerformanceTests
    {
        private enum TestState
        {
            Idle,
            Walk,
        }

        private enum TestEvent
        {
            Go,
            Back,
        }

        private sealed class TestContext { }

        private sealed class DummyState : State<TestContext> { }

        [Test]
        public async Task Send_TransitionLoop_5000Times_CompletesWithinReasonableTime()
        {
            using var machine = CreateMachine();
            await machine.Awake(TestState.Idle);

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 5000; i++)
            {
                var ok1 = await machine.Send(TestEvent.Go);
                var ok2 = await machine.Send(TestEvent.Back);
                Assert.IsTrue(ok1 && ok2);
            }
            sw.Stop();

            TestContext.WriteLine($"Transition loop elapsed: {sw.ElapsedMilliseconds} ms");
            Assert.Less(sw.ElapsedMilliseconds, 2000, "Transition loop is unexpectedly slow in EditMode.");
        }

        [Test]
        public async Task LazySend_QueueDrain_5000Items_CompletesWithinReasonableTime()
        {
            using var machine = CreateMachine();
            await machine.Awake(TestState.Idle);

            for (var i = 0; i < 5000; i++)
            {
                var enqueued = machine.LazySend((i % 2 == 0) ? TestEvent.Go : TestEvent.Back);
                Assert.IsTrue(enqueued);
            }

            var sw = Stopwatch.StartNew();
            await machine.Update(0.016f);
            sw.Stop();

            TestContext.WriteLine($"Queue drain elapsed: {sw.ElapsedMilliseconds} ms");
            Assert.Less(sw.ElapsedMilliseconds, 1500, "Queue drain is unexpectedly slow in EditMode.");
        }

        private static StateMachine<TestContext, TestEvent, TestState> CreateMachine()
        {
            var machine = new StateMachine<TestContext, TestEvent, TestState>(new TestContext());
            machine.RegisterState(TestState.Idle, new DummyState());
            machine.RegisterState(TestState.Walk, new DummyState());
            machine.RegisterTransition(TestState.Idle, TestEvent.Go, TestState.Walk);
            machine.RegisterTransition(TestState.Walk, TestEvent.Back, TestState.Idle);
            return machine;
        }
    }
}
