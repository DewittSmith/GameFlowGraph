using System;

namespace GameFlow.Ports
{
    /// <summary>
    /// Base class for all flow ports in the game flow system.
    /// This abstract class defines the common functionality for both input and output ports.
    /// </summary>
    public abstract class FlowPort
    {
        /// <summary>
        /// Gets the name of this port.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets whether this port is an input port (true) or output port (false).
        /// </summary>
        internal abstract bool IsInput { get; }

        /// <summary>
        /// Gets the type of parameter that this port accepts.
        /// </summary>
        internal abstract Type ParameterType { get; }

        /// <summary>
        /// Gets the result type of this port.
        /// </summary>
        internal abstract Type ResultType { get; }

        /// <summary>
        /// Initializes a new instance of the FlowPort class.
        /// </summary>
        /// <param name="name">The name of the port.</param>
        protected FlowPort(string name)
        {
            Name = name;
        }
    }
}