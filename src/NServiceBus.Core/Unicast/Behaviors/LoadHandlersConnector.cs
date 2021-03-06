﻿namespace NServiceBus
{
    using System;
    using System.Linq;
    using NServiceBus.Unicast.Behaviors;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;

    class LoadHandlersConnector : StageConnector<LogicalMessageProcessingStageBehavior.Context, HandlingStageBehavior.Context>
    {
        readonly MessageHandlerRegistry messageHandlerRegistry;

        public LoadHandlersConnector(MessageHandlerRegistry messageHandlerRegistry)
        {
            this.messageHandlerRegistry = messageHandlerRegistry;
        }

        public override void Invoke(LogicalMessageProcessingStageBehavior.Context context, Action<HandlingStageBehavior.Context> next)
        {
            var handlerTypedToInvoke = messageHandlerRegistry.GetHandlerTypes(context.MessageType).ToList();

            if (!context.MessageHandled && !handlerTypedToInvoke.Any())
            {
                var error = string.Format("No handlers could be found for message type: {0}", context.MessageType);
                throw new InvalidOperationException(error);
            }

            foreach (var handlerType in handlerTypedToInvoke)
            {
                var loadedHandler = new MessageHandler
                {
                    Instance = context.Builder.Build(handlerType),
                    Invocation = (handlerInstance, message) => messageHandlerRegistry.InvokeHandle(handlerInstance, message)
                };

                var handlingContext = new HandlingStageBehavior.Context(loadedHandler, context);
                next(handlingContext);

                if (handlingContext.HandlerInvocationAborted)
                {
                    //if the chain was aborted skip the other handlers
                    break;
                }
            }

            context.MessageHandled = true;
        }
    }
}