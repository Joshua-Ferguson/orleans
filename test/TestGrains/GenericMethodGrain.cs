using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using UnitTests.GrainInterfaces;

using System.Reflection;

namespace UnitTests.Grains
{
    class GenericMethodGrain : Grain, IGenericMethodGrain
    {
        public Task<int> GenericArgument<T>(T data)
        {
            var t = typeof(T).GetTypeInfo();
            if (t == typeof(string).GetTypeInfo())
            {
                var x = (string)(object)data;
                return Task.FromResult(1);
            }
            else if (t == typeof(int).GetTypeInfo())
            {
                var x = (int)(object)data;
                return Task.FromResult(2);
            }
            else if (t == typeof(int[]).GetTypeInfo())
            {
                var x = (int[])(object)data;
                return Task.FromResult(3);
            }
            else if (t == typeof(List<int>).GetTypeInfo())
            {
                var x = (List<int>)(object)data;
                return Task.FromResult(4);
            }

            return Task.FromResult(0);
        }

        public Task<int> GenericTypeParameterOnly<T>()
        {
            var t = typeof(T).GetTypeInfo();
            if (t == typeof(string).GetTypeInfo())
            {
                return Task.FromResult(1);
            }
            else if (t == typeof(int).GetTypeInfo())
            {
                return Task.FromResult(2);
            }
            else if (t == typeof(int[]).GetTypeInfo())
            {
                return Task.FromResult(3);
            }
            else if (t == typeof(List<int>).GetTypeInfo())
            {
                return Task.FromResult(4);
            }

            return Task.FromResult(0);
        }

        private TypeInfo Verify<T>(T t)
        {
            var type = typeof(T).GetTypeInfo();
            var ttype = t.GetType().GetTypeInfo();

            if (type != typeof(object).GetTypeInfo())
            if (type != ttype) throw new Exception($"T typeInfo != t typeInfo ({type} != {ttype})");

            var x = (T)(object)t;
            return type;
        }
        
        public Task<string> MultipleGenericArguments<T, T2, T3>(T t, T2 t2, T3 t3)
        {
            var type1 = Verify(t);
            var type2 = Verify(t2);
            var type3 = Verify(t3);

            if (type1 == type2 && type1 == type3) return Task.FromResult(type1.ToString());
            return Task.FromResult($"{type1}:{type2}:{type3}");
        }

        public Task<T3> MultipleGenericArgumentsOneGenericResult<T, T2, T3>(T t, T2 t2, T3 t3)
        {
            Verify(t);
            Verify(t2);
            Verify(t3);

            return Task.FromResult(t3);
        }

        public Task<T[]> ReturnGenericArray<T>(params T[] values)
        {
            foreach (var value in values) Verify(value);
            return Task.FromResult(values.ToArray());
        }

        public Task<List<T>> ReturnGenericClass<T>(params T[] values)
        {
            foreach (var value in values) Verify(value);
            return Task.FromResult(values.ToList());
        }

        public Task<IEnumerable<T>> ReturnGenericInterface<T>(params T[] values)
        {
            foreach (var value in values) Verify(value);
            return Task.FromResult(values.AsEnumerable());
        }

        public Task<KeyValuePair<TKey, TValue>> ReturnGenericStructure<TKey, TValue>(TKey key, TValue value)
        {
            Verify(key);
            Verify(value);

            return Task.FromResult(new KeyValuePair<TKey, TValue>(key, value));
        }

        public Task<T> UniformGenericArguments<T>(T one, T two)
        {
            Verify(one);
            Verify(two);

            if (typeof(T) == typeof(string))
            {
                var r = (string)(object)one + (string)(object)two;
                return Task.FromResult((T)(object)r);
            }
            else if (typeof(T) == typeof(int))
            {
                var r = (int)(object)one + (int)(object)two;
                return Task.FromResult((T)(object)r);
            }


            throw new Exception($"Type not known. Type was {typeof(T)}.");
        }

        public Task<int> DifferentiateByGenericParametersOnly()
        {
            return Task.FromResult(0);
        }

        public Task<int> DifferentiateByGenericParametersOnly<T>()
        {
            var t = typeof(T).GetTypeInfo();
            if (t == typeof(string).GetTypeInfo())
            {
                return Task.FromResult(1);
            }
            else if (t == typeof(int).GetTypeInfo())
            {
                return Task.FromResult(2);
            }
            else if (t == typeof(int[]).GetTypeInfo())
            {
                return Task.FromResult(3);
            }
            else if (t == typeof(List<int>).GetTypeInfo())
            {
                return Task.FromResult(4);
            }

            return Task.FromResult(5);
        }

        public Task<T2> GenericChangeType<T, T2>(T one) where T : IConvertible where T2 : IConvertible
        {
            return Task.FromResult((T2)Convert.ChangeType(one, typeof(T2)));
        }

        public Task<List<T>> RegularAndConstructedArguments<T>(T arg, List<T> args)
        {
            args.Add(arg);
            return Task.FromResult(args);
        }

        public Task<Dictionary<TKey, TValue>> ComplicatedGeneric<TKey, TValue>(KeyValuePair<TKey, TValue> kvp, Dictionary<TKey, TValue> dictionary)
        {
            dictionary.Add(kvp.Key, kvp.Value);
            return Task.FromResult(dictionary);
        }

        public Task<GenericBlanket<T>> WrapInBlanket<T>(T pig)
        {
            return Task.FromResult(new GenericBlanket<T>(pig));
        }
    }
}
