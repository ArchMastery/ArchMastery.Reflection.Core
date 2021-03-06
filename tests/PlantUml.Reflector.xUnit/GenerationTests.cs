using System;
using System.Linq;
using ArchMastery.Reflector.Core;
using ArchMastery.Reflector.Core.Enums;
using ArchMastery.Reflector.Structurizer;
using Divergic.Logging.Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

#nullable enable
namespace ArchMastery.Structurizer.Reflector.xUnit
{
    public class GenerationTests : LoggingTestsBase
    {
        public GenerationTests(ITestOutputHelper output) : base(output, LogLevel.Debug)
        {
        }

        [Theory]
        [InlineData(typeof(int?))]
        [InlineData(typeof(Guid))]
        [InlineData(typeof(IServiceProvider))]
        [InlineData(typeof(Environment.SpecialFolder))]
        [InlineData(typeof(byte[]))]
        public void BuiltInTypes(Type type)
        {
            TypeHolder<StructurizerClip, StructurizerGenerator> typeHolder = new(type);

            Assert.NotNull(typeHolder);
            var clip = typeHolder.Generate(Layers.All);
            Assert.NotNull(clip);
            Assert.NotEmpty(clip.ToString().ToCharArray());

            typeHolder.Generator.DisplayName.Should().NotBeNullOrWhiteSpace();
            typeHolder.Generator.Slug.Should().NotBeNullOrWhiteSpace();
            typeHolder.Members.Count().Should().Be(clip.Segments.Count);

            Output.WriteLine($"{type.FullName}: {typeHolder.Generator.DisplayName}, {typeHolder.Generator.Slug}");
            Output.WriteLine(new string('-', 80));
            Output.WriteLine($"@startuml\n{clip}\n@enduml");
        }

        [Theory]
        [InlineData(typeof(StringComparer))]
        [InlineData(typeof(string))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(GenerationTests))]
        public void Inheritance(Type type)
        {
            TypeHolder<StructurizerClip, StructurizerGenerator> typeHolder = new(type);

            Assert.NotNull(typeHolder);
            var clip = typeHolder.Generate(Layers.Inheritance);
            Assert.NotNull(clip);
            Assert.NotEmpty(clip.ToString().ToCharArray());

            Output.WriteLine($"@startuml\n{clip}\n@enduml");
        }

        [Theory]
        [InlineData(typeof(TestClass<>), typeof(Extensions), typeof(TestBase<>), typeof(MyEntity))]
        [InlineData(typeof(Extensions), typeof(TestBase<>))]
        public void NonPublicMembers(params Type[] types)
        {
            Output.WriteLine("@startuml");
            foreach (var type in types)
            {
                TypeHolder<StructurizerClip, StructurizerGenerator> typeHolder = new(type);

                Assert.NotNull(typeHolder);
                var clip = typeHolder.Generate(Layers.Type | Layers.NonPublic | Layers.TypeEnd);
                Assert.NotNull(clip);
                var result = clip.ToString();
                Assert.NotEmpty(result);

                Output.WriteLine($"\n{result}");
            }

            Output.WriteLine("\n@enduml");
        }

        [Theory]
        [InlineData(typeof(TestClass<>), typeof(Extensions), typeof(TestBase<>), typeof(MyEntity))]
        [InlineData(typeof(Extensions), typeof(TestBase<>))]
        public void PublicMembers(params Type[] types)
        {
            Output.WriteLine("@startuml");
            foreach (var type in types)
            {
                TypeHolder<StructurizerClip, StructurizerGenerator> typeHolder = new(type);

                Assert.NotNull(typeHolder);
                var clip = typeHolder.Generate(Layers.Type | Layers.NonPublic | Layers.TypeEnd);
                Assert.NotNull(clip);
                var result = clip.ToString();
                Assert.NotEmpty(result);
                Output.WriteLine($"\n{result}");
            }

            Output.WriteLine("\n@enduml");
        }

        [Theory]
        [InlineData(typeof(TestClass<>), typeof(Extensions), typeof(TestBase<>), typeof(MyEntity))]
        [InlineData(typeof(Extensions), typeof(TestBase<>))]
        public void All(params Type[] types)
        {
            Output.WriteLine("@startuml");
            foreach (var type in types)
            {
                TypeHolder<StructurizerClip, StructurizerGenerator> typeHolder = new(type);

                Assert.NotNull(typeHolder);
                var clip = typeHolder.Generate(Layers.TypeEnd);
                Assert.NotNull(clip);
                var result = clip.ToString();
                Assert.NotEmpty(result);
                Output.WriteLine($"{result}\n");
            }

            foreach (var type in types)
            {
                TypeHolder<StructurizerClip, StructurizerGenerator> typeHolder = new(type);

                Assert.NotNull(typeHolder);
                var clip = typeHolder.Generate(Layers.Relationships | Layers.Inheritance);
                Assert.NotNull(clip);
                var result = clip.ToString();
                Assert.NotEmpty(result);
                Output.WriteLine($"{result}\n");
            }

            Output.WriteLine("@enduml");
        }

        [Theory]
        [InlineData(typeof(TestClass<>), typeof(Extensions), typeof(TestBase<>), typeof(MyEntity))]
        [InlineData(typeof(Extensions), typeof(TestBase<>))]
        public void AllWithAttributes(params Type[] types)
        {
            Output.WriteLine("@startuml");
            foreach (var type in types)
            {
                TypeHolder<StructurizerClip, StructurizerGenerator> typeHolder = new(type);

                Assert.NotNull(typeHolder);
                var clip = typeHolder.Generate(Layers.TypeEnd, true);
                Assert.NotNull(clip);
                var result = clip.ToString();
                Assert.NotEmpty(result);
                Output.WriteLine($"{result}\n");
            }

            foreach (var type in types)
            {
                TypeHolder<StructurizerClip, StructurizerGenerator> typeHolder = new(type);

                Assert.NotNull(typeHolder);
                var clip = typeHolder.Generate(Layers.Relationships | Layers.Inheritance, true);
                Assert.NotNull(clip);
                var result = clip.ToString();
                Assert.NotEmpty(result);
                Output.WriteLine($"{result}\n");
            }

            Output.WriteLine("@enduml");
        }
    }
}
