namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Hosting;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    class MoveFaultsToErrorQueueBehavior : PhysicalMessageProcessingStageBehavior
    {
        public MoveFaultsToErrorQueueBehavior(CriticalError criticalError, IDispatchMessages sender, HostInformation hostInformation, BusNotifications notifications, string errorQueueAddress)
        {
            this.criticalError = criticalError;
            this.sender = sender;
            this.hostInformation = hostInformation;
            this.notifications = notifications;
            this.errorQueueAddress = errorQueueAddress;
        }

        public override void Invoke(Context context, Action next)
        {
            try
            {
                next();
            }
            catch (Exception exception)
            {
                try
                {
                    var message = context.PhysicalMessage;

                    Logger.Error("Failed to process message with ID: " + message.Id, exception);
                    message.RevertToOriginalBodyIfNeeded();

                    message.SetExceptionHeaders(exception, PipelineInfo.PublicAddress);

                    message.Headers.Remove(Headers.Retries);


                    message.Headers[Headers.HostId] = hostInformation.HostId.ToString("N");
                    message.Headers[Headers.HostDisplayName] = hostInformation.DisplayName;

                    sender.Dispatch(new OutgoingMessage("msg id",message.Headers,message.Body), new DispatchOptions(errorQueueAddress,new AtomicWithReceiveOperation(), new List<DeliveryConstraint>()));

                    notifications.Errors.InvokeMessageHasBeenSentToErrorQueue(message,exception);
                }
                catch (Exception ex)
                {
                    criticalError.Raise("Failed to forward message to error queue", ex);
                    throw;
                }
            }
        }

        readonly CriticalError criticalError;
        readonly IDispatchMessages sender;
        readonly HostInformation hostInformation;
        readonly BusNotifications notifications;
        readonly string errorQueueAddress;
        static ILog Logger = LogManager.GetLogger<MoveFaultsToErrorQueueBehavior>();

        public class Registration : RegisterStep
        {
            public Registration()
                : base("MoveFaultsToErrorQueue", typeof(MoveFaultsToErrorQueueBehavior), "Invokes the configured fault manager for messages that fails processing (and any retries)")
            {
                InsertBeforeIfExists("HandlerTransactionScopeWrapper");
                InsertBeforeIfExists("FirstLevelRetries");
                InsertBeforeIfExists("SecondLevelRetries");

                InsertBeforeIfExists("ReceivePerformanceDiagnosticsBehavior");

            }
        }
    }
}