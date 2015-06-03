namespace NServiceBus.DeliveryConstraints
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Features;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;

    static class DeliveryConstraintContextExtensions
    {
        public static void AddDeliveryConstraint(this OutgoingContext context, DeliveryConstraint constraint)
        {
            List<DeliveryConstraint> constraints;

            if (!context.TryGet(out constraints))
            {
                constraints = new List<DeliveryConstraint>();

                context.Set(constraints);
            }

            if (constraints.Any(c => c.GetType() == constraint.GetType()))
            {
                throw new InvalidOperationException("Constraint of type " + constraint.GetType().FullName + " already exists");
            }

            constraints.Add(constraint);
        }

        public static bool TryGetDeliveryConstraint<T>(this OutgoingContext context,out  T constraint) where T:DeliveryConstraint
        {
            List<DeliveryConstraint> constraints;

            if (!context.TryGet(out constraints))
            {
               constraints = new List<DeliveryConstraint>();
            }

            return constraints.TryGet(out constraint);
        }

        public static IEnumerable<DeliveryConstraint> GetDeliveryConstraints(this PhysicalOutgoingContextStageBehavior.Context context)
        {
            List<DeliveryConstraint> constraints;

            if (context.TryGet(out constraints))
            {
                return constraints;
            }

            return new List<DeliveryConstraint>();
        }

        public static bool TryGet<T>(this IEnumerable<DeliveryConstraint> list, out T constraint) where T : DeliveryConstraint
        {
            constraint = list.SingleOrDefault(c => c is T) as T;

            return constraint != null;
        }

        public static bool TransportSupportsRestriction<T>(this FeatureConfigurationContext context) where T:DeliveryConstraint
        {
            return context.Settings.Get<TransportDefinition>().GetSupportedDeliveryConstraints().Any(t => t == typeof(T));
        }
    }
}