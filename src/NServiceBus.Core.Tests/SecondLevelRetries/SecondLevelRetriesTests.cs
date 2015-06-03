﻿namespace NServiceBus.Core.Tests.SecondLevelRetries
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using NServiceBus.Faults;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.SecondLevelRetries;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;
    using NUnit.Framework;

    [TestFixture]
    public class SecondLevelRetriesTests
    {
        [Test]
        public void ShouldRetryIfPolicyReturnsADelay()
        {
            var notifications = new BusNotifications();

            var deferrer = new FakeSender();
            var delay = TimeSpan.FromSeconds(5);
            var behavior = new SecondLevelRetriesBehavior(deferrer, new FakePolicy(delay), notifications);
            behavior.Initialize(new PipelineInfo("Test", "test-address-for-this-pipeline"));

            var slrNotification = new SecondLevelRetry();

            notifications.Errors.MessageHasBeenSentToSecondLevelRetries.Subscribe(slr =>
            {
                slrNotification = slr; });

            behavior.Invoke(CreateContext("someid", 1), () => { throw new Exception("testex"); });

            Assert.AreEqual("someid", deferrer.DeferredMessage.Headers[Headers.MessageId]);
            Assert.AreEqual(delay, deferrer.Delay);
            Assert.AreEqual("test-address-for-this-pipeline", deferrer.MessageRoutedTo);

            Assert.AreEqual("testex", slrNotification.Exception.Message);
        }

        [Test]
        public void ShouldSetTimestampHeaderForFirstRetry()
        {
            var deferrer = new FakeSender();
            var delay = TimeSpan.FromSeconds(5);
            var behavior = new SecondLevelRetriesBehavior(deferrer, new FakePolicy(delay),new BusNotifications());
            behavior.Initialize(new PipelineInfo("Test", "test-address-for-this-pipeline"));

            behavior.Invoke(CreateContext("someid", 0), () => { throw new Exception("testex"); });

            Assert.True(deferrer.DeferredMessage.Headers.ContainsKey(SecondLevelRetriesBehavior.RetriesTimestamp));
         }

        [Test]
        public void ShouldSkipRetryIfNoDelayIsReturned()
        {
            var deferrer = new FakeSender();
            var behavior = new SecondLevelRetriesBehavior(deferrer, new FakePolicy(), new BusNotifications());
            behavior.Initialize(new PipelineInfo("Test", "test-address-for-this-pipeline"));
            var context = CreateContext("someid", 1);

            Assert.Throws<Exception>(() => behavior.Invoke(context, () => { throw new Exception("testex"); }));

            Assert.False(context.PhysicalMessage.Headers.ContainsKey(Headers.Retries));
        }
        [Test]
        public void ShouldSkipRetryForDeserializationErrors()
        {
            var deferrer = new FakeSender();
            var behavior = new SecondLevelRetriesBehavior(deferrer, new FakePolicy(TimeSpan.FromSeconds(5)), new BusNotifications());
            behavior.Initialize(new PipelineInfo("Test", "test-address-for-this-pipeline"));
            var context = CreateContext("someid", 1);

            Assert.Throws<MessageDeserializationException>(() => behavior.Invoke(context, () => { throw new MessageDeserializationException("testex"); }));
            Assert.False(context.PhysicalMessage.Headers.ContainsKey(Headers.Retries));
        }

        [Test]
        public void ShouldPullCurrentRetryCountFromHeaders()
        {
            var deferrer = new FakeSender();
            var retryPolicy = new FakePolicy(TimeSpan.FromSeconds(5));

            var behavior = new SecondLevelRetriesBehavior(deferrer, retryPolicy, new BusNotifications());
            behavior.Initialize(new PipelineInfo("Test", "test-address-for-this-pipeline"));

            var currentRetry = 3;

            behavior.Invoke(CreateContext("someid", currentRetry), () => { throw new Exception("testex"); });

            Assert.AreEqual(currentRetry+1, retryPolicy.InvokedWithCurrentRetry);
        }

        [Test]
        public void ShouldDefaultRetryCountToZeroIfNoHeaderIsFound()
        {
            var deferrer = new FakeSender();
            var retryPolicy = new FakePolicy(TimeSpan.FromSeconds(5));
            var context = CreateContext("someid", 2);

            context.PhysicalMessage.Headers.Clear();


            var behavior = new SecondLevelRetriesBehavior(deferrer, retryPolicy, new BusNotifications());
            behavior.Initialize(new PipelineInfo("Test", "test-address-for-this-pipeline"));

            behavior.Invoke(context, () => { throw new Exception("testex"); });

            Assert.AreEqual(1, retryPolicy.InvokedWithCurrentRetry);
            Assert.AreEqual("1", context.PhysicalMessage.Headers[Headers.Retries]);
        }


        PhysicalMessageProcessingStageBehavior.Context CreateContext(string messageId, int currentRetryCount)
        {
            var context = new PhysicalMessageProcessingStageBehavior.Context(new TransportReceiveContext(new IncomingMessage(messageId, new Dictionary<string, string> { { Headers.Retries, currentRetryCount.ToString() } }, new MemoryStream()), null));
            return context;
        }
    }

    class FakePolicy : SecondLevelRetryPolicy
    {
        readonly TimeSpan? delayToReturn;

        public FakePolicy()
        {

        }
        public FakePolicy(TimeSpan delayToReturn)
        {
            this.delayToReturn = delayToReturn;
        }

        public int InvokedWithCurrentRetry { get; private set; }

        public override bool TryGetDelay(TransportMessage message, Exception ex, int currentRetry, out TimeSpan delay)
        {
            InvokedWithCurrentRetry = currentRetry;

            if (!delayToReturn.HasValue)
            {
                delay = TimeSpan.MinValue;
                return false;
            }
            delay = delayToReturn.Value;
            return true;
        }
    }

    class FakeSender : ISendMessages
    {
        public string MessageRoutedTo { get; private set; }

        public OutgoingMessage DeferredMessage { get; private set; }
        public TimeSpan Delay { get; private set; }

       
        public void Send(OutgoingMessage message, TransportSendOptions sendOptions)
        {
            MessageRoutedTo = ((DirectRoutingStrategy)sendOptions.RoutingStrategy).Destination;
            DeferredMessage = message;
          
            var constraint = sendOptions.DeliveryConstraints.SingleOrDefault(c => c is DelayedDelivery) as DelayedDelivery;

            if (constraint != null && constraint.DelayDeliveryWith.HasValue)
            {
                Delay = constraint.DelayDeliveryWith.Value;
            }
        }
    }
}
