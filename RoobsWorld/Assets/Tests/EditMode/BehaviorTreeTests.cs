using NUnit.Framework;
using RoobsWorld.Creature.AI;

namespace RoobsWorld.Tests
{
    public class BehaviorTreeTests
    {
        private class SuccessLeaf : BTNode
        {
            public override BTStatus Tick(BTContext ctx) => BTStatus.Success;
        }

        private class FailLeaf : BTNode
        {
            public override BTStatus Tick(BTContext ctx) => BTStatus.Failure;
        }

        private class RunningLeaf : BTNode
        {
            public int TickCount;
            public override BTStatus Tick(BTContext ctx) { TickCount++; return BTStatus.Running; }
        }

        private class CounterLeaf : BTNode
        {
            public int Count;
            public override BTStatus Tick(BTContext ctx) { Count++; return BTStatus.Success; }
        }

        [Test]
        public void Sequence_Returns_Success_When_All_Children_Succeed()
        {
            var seq = new BTSequence(new SuccessLeaf(), new SuccessLeaf(), new SuccessLeaf());
            Assert.AreEqual(BTStatus.Success, seq.Tick(new BTContext()));
        }

        [Test]
        public void Sequence_Returns_Failure_On_First_Failure()
        {
            var third = new CounterLeaf();
            var seq = new BTSequence(new SuccessLeaf(), new FailLeaf(), third);

            Assert.AreEqual(BTStatus.Failure, seq.Tick(new BTContext()));
            Assert.AreEqual(0, third.Count, "Should not tick children after failure");
        }

        [Test]
        public void Sequence_Returns_Running_And_Resumes()
        {
            var running = new RunningLeaf();
            var seq = new BTSequence(new SuccessLeaf(), running, new SuccessLeaf());

            Assert.AreEqual(BTStatus.Running, seq.Tick(new BTContext()));
            Assert.AreEqual(1, running.TickCount);

            Assert.AreEqual(BTStatus.Running, seq.Tick(new BTContext()));
            Assert.AreEqual(2, running.TickCount);
        }

        [Test]
        public void Selector_Returns_Success_On_First_Success()
        {
            var third = new CounterLeaf();
            var sel = new BTSelector(new FailLeaf(), new SuccessLeaf(), third);

            Assert.AreEqual(BTStatus.Success, sel.Tick(new BTContext()));
            Assert.AreEqual(0, third.Count, "Should not tick children after success");
        }

        [Test]
        public void Selector_Returns_Failure_When_All_Fail()
        {
            var sel = new BTSelector(new FailLeaf(), new FailLeaf());
            Assert.AreEqual(BTStatus.Failure, sel.Tick(new BTContext()));
        }

        [Test]
        public void Selector_Returns_Running_And_Resumes()
        {
            var running = new RunningLeaf();
            var sel = new BTSelector(new FailLeaf(), running);

            Assert.AreEqual(BTStatus.Running, sel.Tick(new BTContext()));
            Assert.AreEqual(BTStatus.Running, sel.Tick(new BTContext()));
            Assert.AreEqual(2, running.TickCount);
        }

        [Test]
        public void Inverter_Flips_Success_To_Failure()
        {
            var inv = new BTInverter(new SuccessLeaf());
            Assert.AreEqual(BTStatus.Failure, inv.Tick(new BTContext()));
        }

        [Test]
        public void Inverter_Flips_Failure_To_Success()
        {
            var inv = new BTInverter(new FailLeaf());
            Assert.AreEqual(BTStatus.Success, inv.Tick(new BTContext()));
        }

        [Test]
        public void Inverter_Passes_Running_Through()
        {
            var inv = new BTInverter(new RunningLeaf());
            Assert.AreEqual(BTStatus.Running, inv.Tick(new BTContext()));
        }

        [Test]
        public void Repeater_Runs_Child_N_Times()
        {
            var counter = new CounterLeaf();
            var repeater = new BTRepeater(counter, 5);
            var ctx = new BTContext();

            for (int i = 0; i < 5; i++)
                repeater.Tick(ctx);

            Assert.AreEqual(5, counter.Count);
        }
    }
}
