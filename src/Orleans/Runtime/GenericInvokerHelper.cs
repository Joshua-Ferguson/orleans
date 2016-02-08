using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Concurrent;

// is this the right namespace? ... note: was originally in Orleans namespace, moving it to Orleans.Runtime (and then adding the using in the codegen files) caused some 
// internal Orleans codegen to change to generic, that was not originally (I noticed due to inserting "grain" and not grain)
namespace Orleans.Runtime
{
    public static class OrleansGenericInvokeHelper
    {
        private static ConcurrentDictionary<string, MethodInfo> invocations = new ConcurrentDictionary<string, MethodInfo>();

        private static string tmp = "";

        private static bool CompareParameters(ParameterInfo[] a, Type[] b)
        {
            if (a == null || b == null) if (a == null && b == null) return true; else return false;
            if (a.Length != b.Length) return false;

            for (int i = 0; i < a.Length; i++)
            {
                var aType = a[i].ParameterType;
                if (!aType.IsGenericParameter && !b[i].IsGenericParameter)
                {
                    if (aType.IsGenericType && b[i].IsGenericType) // todo: this might be an unnecesary check
                    {
                        var aGTD = aType.GetGenericTypeDefinition();
                        var bGTD = b[i].GetGenericTypeDefinition();


                        tmp += $"g{i}:{aGTD} != {bGTD}, ";
                        if (aGTD != bGTD) return false;
                    }
                    else
                    {
                        tmp += $"ng{i}:{aType} != {b[i]}, ";
                        if (aType != b[i]) return false;
                    }
                }
            }

            return true;
        }

        private const char KeyDelimiter = '!';
        private static string GetInvokeKey(Type grainInterface, string methodName, Type[] genericTypeParameters, Type[] argumentTypeParameters) // Can this be faster?
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

        // Should argumentTypeParameters be assummed from the arguments? Probably not.
        public static dynamic Invoke(Type grainInterface, IAddressable grain, string methodName, Type[] genericTypeParameters, object[] arguments, Type[] argumentTypeParameters)
        {
            if (grainInterface == null) return new Exception($"{nameof(OrleansGenericInvokeHelper)}.{nameof(Invoke)}: {nameof(methodName)} = {methodName}; {nameof(grainInterface)} was null.");

            try
            {
                string invokeKey = GetInvokeKey(grainInterface, methodName, genericTypeParameters, argumentTypeParameters);

                MethodInfo invokable;
                bool success = invocations.TryGetValue(invokeKey, out invokable);

                if (!success)
                {
                    var invokables = grainInterface
                        .GetMethods()
                        .Where(i => i.Name == methodName)
                        .Where(i => i.GetGenericArguments().Length == genericTypeParameters.Length)
                        .Where(i => CompareParameters(i.GetParameters(), argumentTypeParameters));

                    if (invokables.Count() != 1) throw new AmbiguousMatchException($"{invokables.Count()} matches found. {tmp}");

                    invokable = invokables
                        .First()
                        .MakeGenericMethod(genericTypeParameters);

                    invocations.TryAdd(invokeKey, invokable);
                }

                return (dynamic)invokable.Invoke(grain, arguments);
            }
            catch (Exception exception)
            {
                throw new Exception($"{nameof(OrleansGenericInvokeHelper)}.{nameof(Invoke)}: ({grainInterface.Namespace}.{grainInterface.Name}) {nameof(methodName)} = {methodName}; ", exception);
            }
        }
    }

}
