﻿namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    class ForwardBehavior : PhysicalMessageProcessingStageBehavior
    {
        public IAuditMessages MessageAuditer { get; set; }

        public string ForwardReceivedMessagesTo { get; set; }


        public override void Invoke(Context context, Action next)
        {
            next();

            context.PhysicalMessage.RevertToOriginalBodyIfNeeded();

            MessageAuditer.Audit(new OutgoingMessage(context.PhysicalMessage.Id,context.PhysicalMessage.Headers,context.PhysicalMessage.Body),
                new TransportSendOptions( ForwardReceivedMessagesTo,new AtomicWithReceiveOperation(), new List<DeliveryConstraint>()));
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("ForwardMessageTo", typeof(ForwardBehavior), "Forwards message to the specified queue in the UnicastBus config section.")
            {
                InsertBefore(WellKnownStep.ExecuteUnitOfWork);
            }
        }
    }
}