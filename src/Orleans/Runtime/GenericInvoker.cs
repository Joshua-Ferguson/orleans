using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Concurrent;

namespace Orleans.Runtime
{
    /// <summary>
    /// This is called from the 'orleans.codegen.cs' generated IGrainMethodInvoker Invoke methods to generate the correct method invoke.
    /// </summary>
    public static class GenericInvoker
    {
        private static ConcurrentDictionary<string, MethodInfo> invocations = new ConcurrentDictionary<string, MethodInfo>();

        private static bool CompareParameters(ParameterInfo[] a, TypeInfo[] b)
        {
            if (a == null || b == null) if (a == null && b == null) return true; else return false;
            if (a.Length != b.Length) return false;

            for (int i = 0; i < a.Length; i++)
            {
                var aType = a[i].ParameterType.GetTypeInfo();
                if (!aType.IsGenericParameter && !b[i].IsGenericParameter)
                {
                    if (aType.IsGenericType && b[i].IsGenericType)
                    {
                        var aGTD = aType.GetGenericTypeDefinition().GetTypeInfo();
                        var bGTD = b[i].GetGenericTypeDefinition().GetTypeInfo();

                        if (aGTD != bGTD) return false;
                    }
                    else
                    {
                        if (aType != b[i]) return false;
                    }
                }
            }

            return true;
        }

        private const char KeyDelimiter = '!';
        private static string GetInvokeKey(Type grainInterface, string methodName, TypeInfo[] genericTypeParameters, TypeInfo[] argumentTypeParameters)
        {
            var sb = new StringBuilder();
            sb.Append(grainInterface.Namespace).Append('.').Append(grainInterface.Name).Append(KeyDelimiter)
                .Append(methodName).Append(KeyDelimiter);

            if (genericTypeParameters != null)
            {
                foreach (var type in genericTypeParameters) sb.Append(type.Namespace).Append('.').Append(type.Name).Append(',');
            }
            sb.Append(KeyDelimiter);

            if (argumentTypeParameters != null)
            {
                foreach (var type in argumentTypeParameters) sb.Append(type.Namespace).Append('.').Append(type.Name).Append(',');
            }
            return sb.ToString();
        }

        // This allows interfaces that specify more interfaces to still be represented
        private static IEnumerable<MethodInfo> GetMethods(TypeInfo t)
        {
            foreach (var method in t.GetMethods()) yield return method;
            foreach (var i in t.GetInterfaces()) foreach (var method in GetMethods(i.GetTypeInfo())) yield return method;
        }

        private static IEnumerable<MethodInfo> GetInvokables(TypeInfo grainInterface, string methodName, TypeInfo[] genericTypeParameters, TypeInfo[] argumentTypeParameters)
        {
            foreach (var method in GetMethods(grainInterface))
            {
                if (method.Name == methodName && method.GetGenericArguments().Length == genericTypeParameters.Length)
                {
                    var constructedMethod = method.MakeGenericMethod(genericTypeParameters);
                    if (CompareParameters(constructedMethod.GetParameters(), argumentTypeParameters)) yield return constructedMethod;
                }
            }
        }

        public static dynamic Invoke(TypeInfo grainInterface, IAddressable grain, string methodName, TypeInfo[] genericTypeParameters, object[] arguments)
        {
            if (grainInterface == null) throw new Exception($"{nameof(GenericInvoker)}.{nameof(Invoke)}: {nameof(methodName)} = {methodName}; {nameof(grainInterface)} was null.");

            try
            {
                TypeInfo[] argumentTypeParameters = arguments?.Select(arg => arg.GetType().GetTypeInfo())?.ToArray() ?? new TypeInfo[0];
                string invokeKey = GetInvokeKey(grainInterface, methodName, genericTypeParameters, argumentTypeParameters);

                MethodInfo invokable;
                bool success = invocations.TryGetValue(invokeKey, out invokable);

                if (!success)
                {
                    // This prevents us from knowing if we find too many invokes
                    var invokables = GetInvokables(grainInterface, methodName, genericTypeParameters, argumentTypeParameters);
                    invokable = invokables.FirstOrDefault();

                    if (invokable == null) throw new AmbiguousMatchException($"0 matches found. genericTypeParameters. Length = {genericTypeParameters.Length}; invokeKey = {invokeKey}; ");

                    invocations.TryAdd(invokeKey, invokable);
                }

                return invokable.Invoke(grain, arguments);
            }
            catch (Exception exception)
            {
                throw new OrleansException($"{nameof(GenericInvoker)}.{nameof(Invoke)} failed trying '(({grainInterface.Namespace}.{grainInterface.Name}){grain.GetType()}) {nameof(methodName)} = {methodName}'", exception);
            }
        }
    }
}
