using System;

namespace GameFlow.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class InjectKeyAttribute : Attribute
    {
        public string Key { get; }

        public InjectKeyAttribute(string key)
        {
            Key = key;
        }
    }
}