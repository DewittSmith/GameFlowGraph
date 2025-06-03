using System;

namespace GameFlow.Attributes
{
    /// <summary>
    /// Hides node from search.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class SearchHiddenAttribute : Attribute
    {
    }
}