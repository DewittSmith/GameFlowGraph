using System;

namespace GameFlow.Ports
{
    /// <summary>
    /// Represents metadata about a port on a flow node, including its name, direction, and type information.
    /// </summary>
    public readonly struct PortInfo
    {
        /// <summary>
        /// Gets the name of this port.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the display name of this port.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets whether this port is an input port (true) or output port (false).
        /// </summary>
        public bool IsInput { get; }

        /// <summary>
        /// Gets the type of parameter that this port accepts.
        /// </summary>
        public Type ParameterType { get; }

        /// <summary>
        /// Gets the result type of this port.
        /// </summary>
        public Type ResultType { get; }

        /// <summary>
        /// Represents method name for AddInvoke override.
        /// </summary>
        public string AddInvokeOverride { get; }

        /// <summary>
        /// Initializes a new instance of the PortInfo struct.
        /// </summary>
        /// <param name="name">The name of the port.</param>
        /// <param name="isInput">Whether this is an input port (true) or output port (false).</param>
        /// <param name="parameterType">The type of parameter this port accepts or provides.</param>
        /// <param name="resultType">The result type of this port.</param>
        public PortInfo(string name, string displayName, bool isInput, Type parameterType, Type resultType, string addInvokeOverride)
        {
            Name = name;
            DisplayName = displayName;
            IsInput = isInput;
            ParameterType = parameterType;
            ResultType = resultType;
            AddInvokeOverride = addInvokeOverride;
        }
    }
}