﻿namespace NServiceBus.Pipeline.Contexts
{
    using System;
    using NServiceBus.Extensibility;

    /// <summary>
    /// 
    /// </summary>
    public abstract class PhysicalOutgoingContextStageBehavior : Behavior<PhysicalOutgoingContextStageBehavior.Context>
    {
        /// <summary>
        /// 
        /// </summary>
        public class Context : BehaviorContext
        {

            /// <summary>
            /// 
            /// </summary>
            /// <param name="body"></param>
            /// <param name="parentContext"></param>
            public Context(byte[] body, OutgoingContext parentContext)
                : base(parentContext)
            {
                Body = body;
                MessageType = parentContext.MessageType;
                Extensions = parentContext.Extensions;
            }

            /// <summary>
            /// The logical message type
            /// </summary>
            public Type MessageType { get; private set; }

            /// <summary>
            /// 
            /// </summary>
            public byte[] Body { get; set; }

            /// <summary>
            /// Place for extensions to store their data
            /// </summary>
            public OptionExtensionContext Extensions { get; private set; }
        }
    }
}