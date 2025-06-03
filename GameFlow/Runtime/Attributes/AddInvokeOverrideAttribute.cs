using System;

namespace GameFlow.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class AddInvokeOverrideAttribute : Attribute
    {
        public string MethodName { get; }

        public AddInvokeOverrideAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}