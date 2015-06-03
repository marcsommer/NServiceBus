namespace NServiceBus.Features
{
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

    /// <summary>
    /// Allows subscribers to register by sending a subscription message to this endpoint
    /// </summary>
    public class MessageDrivenSubscriptions : Feature
    {

        internal MessageDrivenSubscriptions()
        {
            EnableByDefault();
            Prerequisite(c=>!c.Settings.Get<TransportDefinition>().HasNativePubSubSupport,"The transport supports native pub sub");
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent(builder => new SubscriptionManager(builder.Build<Configure>().PublicReturnAddress, builder.Build<ISendMessages>()), DependencyLifecycle.SingleInstance);
        }
    }
}