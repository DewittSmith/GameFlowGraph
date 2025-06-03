using System;
using System.Reflection;
using System.Text;
using GameFlow.Nodes;

namespace GameFlow.Utils
{
    /// <summary>
    /// Provides utility methods for working with types in the game flow editor.
    /// </summary>
    public static class TypeUtils
    {
        public static T GetDefault<T>()
        {
            return default;
        }

        public static object GetDefault(Type type)
        {
            return typeof(TypeUtils).GetRuntimeMethod(nameof(GetDefault), Type.EmptyTypes).MakeGenericMethod(type).Invoke(null, Array.Empty<object>());
        }

        public static ConstructorInfo GetMainConstructor(Type type)
        {
            var ctor = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)[0];
            if (ctor == null)
            {
                throw new("Could not find constructor for type: " + type.FullName);
            }

            return ctor;
        }

        public static IFlowNode CreateDefaultNode(Type type)
        {
            if (type.IsValueType)
            {
                return (IFlowNode)Activator.CreateInstance(type);
            }

            var ctor = GetMainConstructor(type);
            var parameters = ctor.GetParameters();
            if (parameters.Length == 0)
            {
                return (IFlowNode)Activator.CreateInstance(type);
            }

            return (IFlowNode)ctor.Invoke(Array.ConvertAll(parameters, x => GetDefault(x.ParameterType)));
        }

        public static void GetNames(Type type, out string assemblyQualifiedName, out string fullName, out string name)
        {
            if (type == null)
            {
                assemblyQualifiedName = string.Empty;
                fullName = string.Empty;
                name = string.Empty;
                return;
            }

            assemblyQualifiedName = type.AssemblyQualifiedName!;
            fullName = type.FullName!;
            name = type.Name!;

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                GetNames(elementType, out _, out fullName, out name);
                fullName = $"{fullName}[]";
                name = $"{name}[]";
            }
            else if (type.IsGenericType)
            {
                var fullNameBuilder = new StringBuilder();
                var nameBuilder = new StringBuilder();

                fullNameBuilder.Append(fullName.AsSpan(0, fullName.IndexOf('`')));

                int lastPeriod = 0;
                for (int i = 0; i < fullNameBuilder.Length; ++i)
                {
                    if (fullNameBuilder[i] == '.')
                    {
                        lastPeriod = i;
                    }
                }

                nameBuilder.Append(fullNameBuilder, lastPeriod + 1, fullNameBuilder.Length - lastPeriod - 1);

                fullNameBuilder.Append('<');
                nameBuilder.Append('<');

                foreach (var genericArgument in type.GetGenericArguments())
                {
                    GetNames(genericArgument, out _, out string gFullName, out string gName);

                    fullNameBuilder.Append(gFullName);
                    fullNameBuilder.Append(", ");

                    nameBuilder.Append(gName);
                    nameBuilder.Append(", ");
                }

                fullNameBuilder.Remove(fullNameBuilder.Length - 2, 2);
                nameBuilder.Remove(nameBuilder.Length - 2, 2);

                fullNameBuilder.Append('>');
                nameBuilder.Append('>');

                fullName = fullNameBuilder.ToString();
                name = nameBuilder.ToString();
            }
        }
    }
}