using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFlow.Utils
{
    /// <summary>
    /// Provides color mapping for different types in the game flow editor.
    /// Used to visually distinguish between different port types in the graph view.
    /// </summary>
    public static class TypeColors
    {
        private static Color defaultColor;
        private static readonly Dictionary<Type, Color> Colors = new();

        /// <summary>
        /// Initializes the type colors with default values for common types.
        /// </summary>
        static TypeColors()
        {
            SetDefaultColor(new(0.8f, 0.8f, 0.8f));
            SetColor(typeof(int), new(0.0f, 0.941f, 1.0f));
            SetColor(typeof(float), new(0.0f, 0.941f, 1.0f));
            SetColor(typeof(bool), new(0.702f, 0.416f, 1.0f));
            SetColor(typeof(Vector2), new(0.392f, 1.0f, 0.624f));
            SetColor(typeof(Vector3), new(1.0f, 0.976f, 0.392f));
            SetColor(typeof(Vector4), new(1.0f, 0.392f, 0.976f));
            SetColor(typeof(Color), new(1.0f, 0.392f, 0.976f));
            SetColor(typeof(string), new(1.0f, 0.353f, 0.353f));
        }

        /// <summary>
        /// Sets the default color to use for types that don't have a specific color assigned.
        /// </summary>
        /// <param name="color">The color to use as the default.</param>
        public static void SetDefaultColor(Color color)
        {
            defaultColor = color;
        }

        /// <summary>
        /// Sets a specific color for a type.
        /// </summary>
        /// <param name="type">The type to set the color for.</param>
        /// <param name="color">The color to assign to the type.</param>
        public static void SetColor(Type type, Color color)
        {
            Colors[type] = color;
        }

        /// <summary>
        /// Gets the color associated with a type, or the default color if no specific color is set.
        /// </summary>
        /// <param name="type">The type to get the color for.</param>
        /// <returns>The color associated with the type, or the default color if not found.</returns>
        public static Color GetColor(Type type)
        {
            return Colors.GetValueOrDefault(type, defaultColor);
        }
    }
}