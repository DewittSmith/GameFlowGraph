using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameFlow.Attributes;
using GameFlow.Nodes;
using GameFlow.Ports;
using UnityEditor;

namespace GameFlow.Utils
{
    /// <summary>
    /// Provides utility methods for working with flow nodes and their ports.
    /// </summary>
    public static class FlowNodeUtils
    {
        private static readonly Type PortType = typeof(FlowPort);

        /// <summary>
        /// Finds all concrete node types that implement IFlowNode and can be instantiated.
        /// </summary>
        /// <returns>An enumerable of all valid node types that can be used in the game flow system.</returns>
        /// <remarks>
        /// A valid node type must:
        /// - Implement IFlowNode
        /// - Not be abstract
        /// - Not be an interface
        /// - Have a parameterless constructor
        /// </remarks>
        public static IEnumerable<Type> FindAllNodeTypes()
        {
            return TypeCache.GetTypesDerivedFrom<IFlowNode>().Where(type => !type.IsAbstract && !type.IsInterface);
        }

        /// <summary>
        /// Finds all ports on a flow node instance, including ports in arrays.
        /// </summary>
        /// <param name="node">The node instance to find ports on.</param>
        /// <returns>An enumerable of port information for all ports found on the node.</returns>
        /// <remarks>
        /// This method will:
        /// - Find all properties that are FlowPort instances
        /// - Find all properties that are arrays of FlowPort
        /// - For array properties, create port info for each element with indexed names
        /// - Use the port's Name property if set, otherwise use the property name
        /// </remarks>
        public static IEnumerable<PortInfo> FindPorts(IFlowNode node)
        {
            foreach (var propertyInfo in node.GetType().GetRuntimeProperties())
            {
                if (PortType.IsAssignableFrom(propertyInfo.PropertyType))
                {
                    var port = (FlowPort)propertyInfo.GetValue(node);
                    string displayName = port.Name ?? propertyInfo.Name;

                    string addInvokeOverride = propertyInfo.GetCustomAttribute<AddInvokeOverrideAttribute>()?.MethodName;
                    var portInfo = new PortInfo(propertyInfo.Name, displayName, port.IsInput, port.ParameterType, port.ResultType, addInvokeOverride);
                    yield return portInfo;
                }
                else if (propertyInfo.PropertyType.IsArray)
                {
                    var elementType = propertyInfo.PropertyType.GetElementType()!;
                    if (!PortType.IsAssignableFrom(elementType))
                    {
                        continue;
                    }

                    var array = (Array)propertyInfo.GetValue(node);
                    string addInvokeOverride = propertyInfo.GetCustomAttribute<AddInvokeOverrideAttribute>()?.MethodName;

                    int i = 0;
                    foreach (object o in array)
                    {
                        var port = (FlowPort)o;
                        string name = port.Name ?? $"{propertyInfo.Name}[{i}]";

                        var portInfo = new PortInfo(name, name, port.IsInput, port.ParameterType, port.ResultType, addInvokeOverride);
                        yield return portInfo;

                        ++i;
                    }
                }
            }
        }
    }
}