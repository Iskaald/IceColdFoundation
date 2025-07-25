using System;

namespace IceColdCore.Interface
{
    /// <summary>
    /// Defines the initialization and deinitialization priority for a core service.
    /// Lower numbers are initialized first and deinitialized last.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ServicePriorityAttribute : Attribute
    {
        public int Priority { get; }

        public ServicePriorityAttribute(int priority)
        {
            Priority = priority;
        }
    }
}