using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace InterfaceIssue
{
    [TestClass]
    public sealed class Test1
    {
        public async Task PerformTest<T>() where T : class, new()
        {
            await using var provider = new ServiceCollection()
                .AddMassTransitTestHarness(x =>
                {
                    x.AddConsumer<AlwaysFailConsumer<T>>();
                    x.AddConsumer<FaultMessageConsumer<T>>();
                    x.AddConsumer<FaultMessageConsumer<IMyMessageInterface>>();
                })
                .BuildServiceProvider();

            var harness = provider.GetRequiredService<ITestHarness>();

            await harness.Start();

            await harness.Bus.Publish(new T());

            var messagePublished = await harness.Published.Any<T>();
            Assert.IsTrue(messagePublished);

            var messageConsumed = await harness.Consumed.Any<T>();
            Assert.IsTrue(messageConsumed);

            var consumedMessage = harness.Consumed.Select<T>().FirstOrDefault();
            Assert.IsNotNull(consumedMessage);
            Assert.IsNotNull(consumedMessage.Exception);

            var faultMessagePublished = await harness.Published.Any<Fault<T>>();
            Assert.IsTrue(faultMessagePublished);

            var faultInterfaceMessagePublished = await harness.Published.Any<Fault<IMyMessageInterface>>();
            Assert.IsTrue(faultInterfaceMessagePublished);

            await Task.Delay(5000);

            var consumerHarness = harness.GetConsumerHarness<FaultMessageConsumer<T>>();
            Assert.IsTrue(await consumerHarness.Consumed.Any<Fault<T>>());

            var interfaceConsumerHarness = harness.GetConsumerHarness<FaultMessageConsumer<IMyMessageInterface>>();
            Assert.IsTrue(await interfaceConsumerHarness.Consumed.Any<Fault<IMyMessageInterface>>());
        }

        [TestMethod]
        public async Task TestMessageHasInterfaceAndExcludedFromTopologyBaseClassDoesNot()
        {
            await PerformTest<MessageHasInterfaceAndExcludedFromTopologyBaseClassDoesNot>();
        }

        [TestMethod]
        public async Task TestMessageHasInterfaceAndIncludedInTopologyBaseClassDoesNot()
        {
            await PerformTest<MessageHasInterfaceAndIncludedInTopologyBaseClassDoesNot>();
        }

        [TestMethod]
        public async Task TestMessageDoesNotHaveInterfaceAndExcludedFromTopologyBaseClassDoes()
        {
            await PerformTest<MessageDoesNotHaveInterfaceAndExcludedFromTopologyBaseClassDoes>();
        }

        [TestMethod]
        public async Task TestMessageDoesNotHaveInterfaceAndIncludedInTopologyBaseHasInterface()
        {
            await PerformTest<MessageDoesNotHaveInterfaceAndIncludedInTopologyBaseHasInterface>();
        }

        [TestMethod]
        public async Task TestMessageHasInterfaceAndDoesNotHaveBaseClass()
        {
            await PerformTest<MessageHasInterfaceAndDoesNotHaveBaseClass>();
        }

        [TestMethod]
        public async Task TestMessageHasInterfaceAndIncludedInTopologyBaseHasInterface()
        {
            await PerformTest<MessageHasInterfaceAndIncludedInTopologyBaseHasInterface>();
        }

        [TestMethod]
        public async Task TestMessageHasInterfaceAndExcludedFromTopologyBaseHasInterface()
        {
            await PerformTest<MessageHasInterfaceAndExcludedFromTopologyBaseHasInterface>();
        }
    }

    public interface IMyMessageInterface
    {
        Guid CorrelationId { get; }
    }

    public interface IMyGeneralMessageInterface : IMyMessageInterface
    {
        string message { get; }
    }

    [ExcludeFromTopology]
    public abstract class BaseClassExcludedFromTopologyHavingNoInterface
    {
        public virtual Guid CorrelationId { get; set; } = Guid.NewGuid();
    }

    [ExcludeFromTopology]
    public abstract class BaseClassExcludedFromTopologyHavingInterface : IMyMessageInterface
    {
        public virtual Guid CorrelationId { get; set; } = Guid.NewGuid();
    }

    public abstract class BaseClassNotExcludedFromTopologyHavingNoInterface
    {
        public virtual Guid CorrelationId { get; set; } = Guid.NewGuid();
    }

    public abstract class BaseClassNotExcludedFromTopologyHavingInterface : IMyMessageInterface
    {
        public virtual Guid CorrelationId { get; set; } = Guid.NewGuid();
    }

    public class MessageHasInterfaceAndExcludedFromTopologyBaseClassDoesNot : BaseClassExcludedFromTopologyHavingNoInterface, IMyMessageInterface
    {
        public virtual string message { get; set; } = string.Empty;
    }

    public class MessageHasInterfaceAndIncludedInTopologyBaseClassDoesNot : BaseClassNotExcludedFromTopologyHavingNoInterface, IMyMessageInterface
    {
        public virtual string message { get; set; } = string.Empty;
    }

    public class MessageDoesNotHaveInterfaceAndExcludedFromTopologyBaseClassDoes : BaseClassExcludedFromTopologyHavingInterface
    {
        public virtual string message { get; set; } = string.Empty;
    }

    public class MessageDoesNotHaveInterfaceAndIncludedInTopologyBaseHasInterface : BaseClassNotExcludedFromTopologyHavingInterface
    {
        public virtual string message { get; set; } = string.Empty;
    }

    public class MessageHasInterfaceAndIncludedInTopologyBaseHasInterface : BaseClassNotExcludedFromTopologyHavingInterface, IMyMessageInterface
    {
        public virtual string message { get; set; } = string.Empty;
    }

    public class MessageHasInterfaceAndExcludedFromTopologyBaseHasInterface : BaseClassExcludedFromTopologyHavingInterface, IMyMessageInterface
    {
        public virtual string message { get; set; } = string.Empty;
    }


    public class MessageHasInterfaceAndDoesNotHaveBaseClass : IMyMessageInterface
    {
        public virtual Guid CorrelationId { get; set; } = Guid.NewGuid();
        public virtual string message { get; set; } = string.Empty;
    }

    public class AlwaysFailConsumer<T> : IConsumer<T> where T : class
    {
        public Task Consume(ConsumeContext<T> context)
        {
            throw new Exception("Fail on purpose");
        }
    }

    public class FaultMessageConsumer<T> : IConsumer<Fault<T>> where T : class
    {
        public Task Consume(ConsumeContext<Fault<T>> context)
        {
            Console.WriteLine($"Consuming Fault<{typeof(T)}>");
            return Task.CompletedTask;
        }
    }

    public class Test : IConsumer<Fault<MessageHasInterfaceAndExcludedFromTopologyBaseClassDoesNot>>
    {
        public Task Consume(ConsumeContext<Fault<MessageHasInterfaceAndExcludedFromTopologyBaseClassDoesNot>> context)
        {
            Console.WriteLine($"Consuming Fault<MessageHasInterfaceAndExcludedFromTopologyBaseClassDoesNot>");
            return Task.CompletedTask;
        }
    }
}
