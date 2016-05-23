using System;
using System.Globalization;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using UnitTests.GrainInterfaces;
using UnitTests.Tester;
using System.Collections.Generic;
using TestGrainInterfaces;
using Xunit;
using Tester;
using System.Linq;
using System.Reflection;
using Orleans.CodeGeneration;
using Orleans.Serialization;

namespace UnitTests.General
{
    /// <summary>
    /// Unit tests for grains implementing methods with generic type parameters.
    /// </summary>
    public class GenericMethodGrainTests : HostedTestClusterEnsureDefaultStarted
    {
        [Fact, TestCategory("Functional"), TestCategory("Generics")]
        public async Task GenericArgumentTypeResolvesCorrectly()
        {
            var grain = GrainClient.GrainFactory.GetGrain<IGenericMethodGrain>(0);
            Assert.Equal(0, await grain.GenericArgument<object>(3.14159));
            Assert.Equal(0, await grain.GenericArgument<object>("hello orleans"));
        }


        [Fact, TestCategory("Functional"), TestCategory("Generics")]
        public async Task GenericArgumentTypeWorks()
        {
            var grain = GrainClient.GrainFactory.GetGrain<IGenericMethodGrain>(0);
            Assert.Equal(1, await grain.GenericArgument("hello orleans"));
            Assert.Equal(2, await grain.GenericArgument(3));
            Assert.Equal(3, await grain.GenericArgument(new int[] { 1, 2, 3 }));
            Assert.Equal(4, await grain.GenericArgument(new List<int>()));
            Assert.Equal(0, await grain.GenericArgument(3.14159));
        }

        [Fact, TestCategory("Functional"), TestCategory("Generics")]
        public async Task GenericTypeParameterWorks()
        {
            var grain = GrainClient.GrainFactory.GetGrain<IGenericMethodGrain>(0);
            Assert.Equal(1, await grain.GenericTypeParameterOnly<string>());
            Assert.Equal(2, await grain.GenericTypeParameterOnly<int>());
            Assert.Equal(3, await grain.GenericTypeParameterOnly<int[]>());
            Assert.Equal(4, await grain.GenericTypeParameterOnly<List<int>>());
            Assert.Equal(0, await grain.GenericTypeParameterOnly<double>());
        }

        [Fact, TestCategory("Functional"), TestCategory("Generics")]
        public async Task ManyGenericArgumentsWork()
        {
            var grain = GrainClient.GrainFactory.GetGrain<IGenericMethodGrain>(0);
            Assert.Equal(typeof(string).GetTypeInfo().ToString(), await grain.MultipleGenericArguments("talks into mouse", "hello", "orleans"));
            Assert.Equal($"{typeof(int).GetTypeInfo()}:{typeof(byte).GetTypeInfo()}:{typeof(ushort).GetTypeInfo()}", await grain.MultipleGenericArguments(1, (byte)2, (ushort)3));
        }

        [Fact, TestCategory("Functional"), TestCategory("Generics")]
        public async Task ManyGenericArgumentsOneGenericResultWorks()
        {
            var grain = GrainClient.GrainFactory.GetGrain<IGenericMethodGrain>(0);
            Assert.Equal(await grain.MultipleGenericArgumentsOneGenericResult("talks into mouse", "hello", "orleans"), "orleans");
            Assert.Equal(await grain.MultipleGenericArgumentsOneGenericResult(1, (byte)2, (ushort)3), (ushort)3);
        }

        [Fact, TestCategory("Functional"), TestCategory("Generics")]
        public async Task ReturnGenericArrayWorks()
        {
            var grain = GrainClient.GrainFactory.GetGrain<IGenericMethodGrain>(0);
            Assert.Equal(new string[] { "talks into mouse", "hello", "orleans" }, await grain.ReturnGenericArray("talks into mouse", "hello", "orleans"));
            // This may not work, like ever
            Assert.Equal(new object[] { 1, (byte)2, (ushort)3 }, await grain.ReturnGenericArray<object>(1, (byte)2, (ushort)3));
        }

        [Fact, TestCategory("Functional"), TestCategory("Generics")]
        public async Task ReturnGenericClassWorks()
        {
            var grain = GrainClient.GrainFactory.GetGrain<IGenericMethodGrain>(0);
            Assert.Equal(new List<string>(new string[] { "talks into mouse", "hello", "orleans" }), await grain.ReturnGenericClass("talks into mouse", "hello", "orleans"));
        }

        [Fact, TestCategory("Functional"), TestCategory("Generics")]
        public async Task ReturnGenericInterfaceWorks()
        {
            var grain = GrainClient.GrainFactory.GetGrain<IGenericMethodGrain>(0);
            Assert.Equal(new List<string>(new string[] { "talks into mouse", "hello", "orleans" }).AsEnumerable(), await grain.ReturnGenericClass("talks into mouse", "hello", "orleans"));
        }

        [Fact, TestCategory("Functional"), TestCategory("Generics")]
        public async Task ReturnGenericStructureWorks()
        {
            var grain = GrainClient.GrainFactory.GetGrain<IGenericMethodGrain>(0);
            Assert.Equal(new KeyValuePair<string, string>("hello", "orleans"), await grain.ReturnGenericStructure("hello", "orleans"));
        }

        [Fact, TestCategory("Functional"), TestCategory("Generics")]
        public async Task UniformGenericArgumentsWorks()
        {
            var grain = GrainClient.GrainFactory.GetGrain<IGenericMethodGrain>(0);
            Assert.Equal("helloorleans", await grain.UniformGenericArguments("hello", "orleans"));
            Assert.Equal(3, await grain.UniformGenericArguments(1, 2));
        }

        [Fact, TestCategory("Functional"), TestCategory("Generics")]
        public async Task DifferentiateByGenericParametersOnlyWorks()
        {
            var grain = GrainClient.GrainFactory.GetGrain<IGenericMethodGrain>(0);
            Assert.Equal(0, await grain.DifferentiateByGenericParametersOnly());
            Assert.Equal(1, await grain.DifferentiateByGenericParametersOnly<string>());
            Assert.Equal(2, await grain.DifferentiateByGenericParametersOnly<int>());
            Assert.Equal(3, await grain.DifferentiateByGenericParametersOnly<int[]>());
            Assert.Equal(4, await grain.DifferentiateByGenericParametersOnly<List<int>>());
            Assert.Equal(5, await grain.DifferentiateByGenericParametersOnly<double>());
        }

        //[Fact, TestCategory("Functional"), TestCategory("Generics")]
        //public async Task GenericChangeTypeWorks()
        //{
        //    var grain =  GrainClient.GrainFactory.GetGrain<IGenericMethodGrain>(0);
        //    Assert.AreEqual(await grain.GenericChangeType<int, double>(2), 2.0);
        //}

        [Fact, TestCategory("Functional"), TestCategory("Generics")]
        public async Task RegularAndConstructedArgumentsWorks()
        {
            var grain = GrainClient.GrainFactory.GetGrain<IGenericMethodGrain>(0);
            Assert.Equal(new List<int>(new int[] { 1, 2, 3 }), await grain.RegularAndConstructedArguments(3, new List<int>(new int[] { 1, 2 })));
        }

        [Fact, TestCategory("Functional"), TestCategory("Generics")]
        public async Task ComplicatedGenericWorks()
        {
            var grain = GrainClient.GrainFactory.GetGrain<IGenericMethodGrain>(0);
            var kvp = new KeyValuePair<int, int>(2, 12);

            var dict = new Dictionary<int, int>();
            dict.Add(0, 10);
            dict.Add(1, 11);

            var biggerDict = await grain.ComplicatedGeneric(kvp, dict);

            Assert.Equal(3, biggerDict.Count());
            Assert.Equal(10, biggerDict[0]);
            Assert.Equal(11, biggerDict[1]);
            Assert.Equal(12, biggerDict[2]);
        }

        [Fact, TestCategory("Functional"), TestCategory("Generics")]
        public async Task WrapInGenericWorks()
        {
            var grain = GrainClient.GrainFactory.GetGrain<IGenericMethodGrain>(0);

            var blanket = await grain.WrapInBlanket(3);
            Assert.Equal(3, blanket.Pig);
        }
    }
}
