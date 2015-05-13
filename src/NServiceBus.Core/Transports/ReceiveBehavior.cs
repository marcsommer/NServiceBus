namespace NServiceBus.Transports
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    /// <summary>
    /// 
    /// </summary>
    public abstract class ReceiveBehavior : StageConnector<IncomingContext, TransportReceiveContext>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        public override Task Invoke(IncomingContext context, Func<TransportReceiveContext, Task> next)
        {
            return Invoke(context, x => next(new TransportReceiveContext(x, context)));
        }

        //TODO: change to header and body ony
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="onMessage"></param>
        protected abstract Task Invoke(IncomingContext context, Func<IncomingMessage, Task> onMessage);

        /// <summary>
        /// 
        /// </summary>
        public class Registration : RegisterStep
        {
            /// <summary>
            /// 
            /// </summary>
            public Registration(): base("ReceiveMessage", typeof(ReceiveBehavior), "Try receive message from transport")
            {
            }
        }
    }
}