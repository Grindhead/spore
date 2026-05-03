using NUnit.Framework;
using RoobsWorld.Core;

namespace RoobsWorld.Tests
{
    public class EventBusTests
    {
        private struct TestEvent : IGameEvent
        {
            public int Value;
        }

        private struct OtherEvent : IGameEvent
        {
        }

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        [Test]
        public void Subscribe_And_Publish_Delivers_Event()
        {
            int received = -1;
            EventBus.Subscribe<TestEvent>(e => received = e.Value);

            EventBus.Publish(new TestEvent { Value = 42 });

            Assert.AreEqual(42, received);
        }

        [Test]
        public void Unsubscribe_Stops_Delivery()
        {
            int received = 0;
            void Handler(TestEvent e) => received = e.Value;

            EventBus.Subscribe<TestEvent>(Handler);
            EventBus.Publish(new TestEvent { Value = 1 });
            Assert.AreEqual(1, received);

            EventBus.Unsubscribe<TestEvent>(Handler);
            EventBus.Publish(new TestEvent { Value = 2 });
            Assert.AreEqual(1, received);
        }

        [Test]
        public void Multiple_Subscribers_All_Receive_Event()
        {
            int countA = 0, countB = 0;
            EventBus.Subscribe<TestEvent>(_ => countA++);
            EventBus.Subscribe<TestEvent>(_ => countB++);

            EventBus.Publish(new TestEvent { Value = 1 });

            Assert.AreEqual(1, countA);
            Assert.AreEqual(1, countB);
        }

        [Test]
        public void Different_Event_Types_Are_Independent()
        {
            int testCount = 0;
            int otherCount = 0;
            EventBus.Subscribe<TestEvent>(_ => testCount++);
            EventBus.Subscribe<OtherEvent>(_ => otherCount++);

            EventBus.Publish(new TestEvent { Value = 1 });

            Assert.AreEqual(1, testCount);
            Assert.AreEqual(0, otherCount);
        }

        [Test]
        public void Clear_Removes_All_Subscribers()
        {
            int count = 0;
            EventBus.Subscribe<TestEvent>(_ => count++);
            EventBus.Clear();
            EventBus.Publish(new TestEvent { Value = 1 });

            Assert.AreEqual(0, count);
        }

        [Test]
        public void HasSubscribers_Returns_Correct_State()
        {
            Assert.IsFalse(EventBus.HasSubscribers<TestEvent>());

            void Handler(TestEvent e) { }
            EventBus.Subscribe<TestEvent>(Handler);
            Assert.IsTrue(EventBus.HasSubscribers<TestEvent>());

            EventBus.Unsubscribe<TestEvent>(Handler);
            Assert.IsFalse(EventBus.HasSubscribers<TestEvent>());
        }

        [Test]
        public void Publish_Without_Subscribers_Does_Not_Throw()
        {
            Assert.DoesNotThrow(() => EventBus.Publish(new TestEvent { Value = 99 }));
        }
    }
}
