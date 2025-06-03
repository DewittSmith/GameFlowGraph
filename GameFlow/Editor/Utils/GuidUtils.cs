using UnityEditor;

namespace GameFlow.Editor.Utils
{
    /// <summary>
    /// Provides utility methods for working with GUIDs in the game flow editor.
    /// </summary>
    public static class GuidUtils
    {
        /// <summary>
        /// Gets a zero GUID string (all zeros).
        /// </summary>
        public static string Zero { get; } = new GUID().ToString();

        /// <summary>
        /// Generates a new unique GUID string.
        /// </summary>
        /// <returns>A new unique GUID as a string.</returns>
        public static string Generate()
        {
            return GUID.Generate().ToString();
        }
    }
}