using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using Orleans.CodeGeneration;
using Orleans.Serialization;

namespace UnitTests.GrainInterfaces
{
    public class GenericBlanket<T>
    {
        // For testing, The pig must be an Int
        public T Pig;

        public GenericBlanket(T pig)
        {
            this.Pig = pig;
        }
    }

    [Serializer(typeof(GenericBlanket<>))]
    internal class GenericBlanketTSerialization<T>
    {

        public static object Copy(object input)
        {
            var original = (GenericBlanket<T>)input;
            return new GenericBlanket<T>(original.Pig);
        }

        public static void Serialize(object obj, BinaryTokenStreamWriter stream, Type expected)
        {
            var input = (GenericBlanket<T>)obj;
            var bytes = BitConverter.GetBytes((int)(object)input.Pig);

            stream.Write(bytes);
        }

        public static object Deserialize(Type expected, BinaryTokenStreamReader stream)
        {
            int pig = stream.ReadInt();
            return new GenericBlanket<T>((T)(object)pig);
        }
    }

    [RegisterSerializer]
    internal class GenericBlanketTSerializerRegister
    {
        static GenericBlanketTSerializerRegister()
        {
            Register();
        }

        public static void Register()
        {
            SerializationManager.Register(typeof(GenericBlanket<>), typeof(GenericBlanketTSerialization<>));
        }
    }

    public interface IGenericMethodGrain : IGrainWithIntegerKey
    {
        Task<int> GenericArgument<T>(T data);
        Task<int> GenericTypeParameterOnly<T>();
        Task<string> MultipleGenericArguments<T, T2, T3>(T t, T2 t2, T3 t3);
        Task<T3> MultipleGenericArgumentsOneGenericResult<T, T2, T3>(T t, T2 t2, T3 t3);
        Task<T> UniformGenericArguments<T>(T one, T two);

        Task<List<T>> RegularAndConstructedArguments<T>(T arg, List<T> args);
        Task<Dictionary<TKey, TValue>> ComplicatedGeneric<TKey, TValue>(KeyValuePair<TKey, TValue> kvp, Dictionary<TKey, TValue> dictionary);

        Task<KeyValuePair<TKey, TValue>> ReturnGenericStructure<TKey, TValue>(TKey key, TValue value);
        Task<IEnumerable<T>> ReturnGenericInterface<T>(params T[] values);
        Task<List<T>> ReturnGenericClass<T>(params T[] values);
        Task<T[]> ReturnGenericArray<T>(params T[] values);

        Task<int> DifferentiateByGenericParametersOnly();
        Task<int> DifferentiateByGenericParametersOnly<T>();

        Task<GenericBlanket<T>> WrapInBlanket<T>(T pig);

        // This one also requires generic constraint support
        //Task<T2> GenericChangeType<T, T2>(T one) where T : IConvertible where T2 : IConvertible;
    }
}
