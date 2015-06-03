namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Features;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Transports;
    using NServiceBus.Transports.Msmq;

    /// <summary>
    /// Transport definition for MSMQ
    /// </summary>
    public class MsmqTransport : TransportDefinition
    {
        /// <summary>
        /// Ctor
        /// </summary>
        public MsmqTransport()
        {
            RequireOutboxConsent = true;
            HasSupportForMultiQueueNativeTransactions = true;
        }

        /// <summary>
        /// Gives implementations access to the <see cref="BusConfiguration"/> instance at configuration time.
        /// </summary>
        protected internal override void Configure(BusConfiguration config)
        {
            // For MSMQ the endpoint differentiator is a no-op since you commonly scale out by running the same endpoint on a different machine.
            // if users want to run more than one instance on the same machine they need to set an explicit discriminator
            config.GetSettings()
                .SetDefault("EndpointInstanceDiscriminator", String.Empty);
               
            config.EnableFeature<MsmqTransportConfigurator>();
        }

        /// <summary>
        /// <see cref="TransportDefinition.GetSubScope"/>
        /// </summary>
        public override string GetSubScope(string address, string qualifier)
        {
            Guard.AgainstNullAndEmpty(address, "address");
            Guard.AgainstNullAndEmpty(qualifier, "qualifier");

            var msmqAddress = MsmqAddress.Parse(address);

            return msmqAddress.ToString(qualifier);
        }
        /// <summary>
        /// The list of constraints supported by the MSMQ transport
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<Type> GetSupportedDeliveryConstraints()
        {
            return new[]
            {
                typeof(DiscardIfNotReceivedBefore)
            };
        }
    }
}