using System;

namespace GameFlow.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class GenerateGenericSubclassesAttribute : Attribute
    {
        public Type ParameterTypeBase { get; set; }
        public Type ResultTypeBase { get; set; }
    }
}