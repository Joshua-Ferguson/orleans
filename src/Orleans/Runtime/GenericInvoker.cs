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
                var aType = a[i].ParameterType;
                if (!aType.IsGenericParameter && !b[i].IsGenericParameter)
                {
                    if (aType.IsGenericType && b[i].IsGenericType) // TODO: is this redundant?
                    {
                        var aGTD = aType.GetGenericTypeDefinition();
                        var bGTD = b[i].GetGenericTypeDefinition();

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
        private static string GetInvokeKey(Type grainInterface, string methodName, TypeInfo[] genericTypeParameters, TypeInfo[] argumentTypeParameters) // TODO: Can this be better?
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
        private static List<MethodInfo> GetMethods(TypeInfo t)
        {
            var methods = new List<MethodInfo>();
            methods.AddRange(t.GetMethods());

            foreach (var i in t.GetInterfaces()) methods.AddRange(GetMethods(i.GetTypeInfo()));

            return methods;
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
                    var invokables = GetMethods(grainInterface)
                        .Where(i => i.Name == methodName)
                        .Where(i => i.GetGenericArguments().Length == genericTypeParameters.Length)
                        .Where(i => CompareParameters(i.MakeGenericMethod(genericTypeParameters).GetParameters(), argumentTypeParameters));

                    if (invokables.Count() != 1) throw new AmbiguousMatchException($"{invokables.Count()} matches found. genericTypeParameters.Length = {genericTypeParameters.Length}; invokeKey = {invokeKey}; ");

                    invokable = invokables
                        .First()
                        .MakeGenericMethod(genericTypeParameters);

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
