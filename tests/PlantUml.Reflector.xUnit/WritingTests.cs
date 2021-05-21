using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ArchMastery.Reflector.Core;
using ArchMastery.Reflector.Core.Enums;
using Divergic.Logging.Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

#nullable enable
namespace ArchMastery.Structurizer.Reflector.xUnit
{
    public class WritingTests : LoggingTestsBase
    {
        private readonly string _writeFileAsync;
        private readonly string _writeFileSync;

        public WritingTests(ITestOutputHelper output) : base(output, LogLevel.Debug)
        {
            const string outputWriteFileSyncStructurizer = "output/writeFileSync.structurizer";
            const string outputWriteFileAsyncStructurizer = "output/writeFileAsync.structurizer";

            _writeFileSync = Path.Combine(Environment.CurrentDirectory, outputWriteFileSyncStructurizer);
            _writeFileAsync = Path.Combine(Environment.CurrentDirectory, outputWriteFileAsyncStructurizer);

            if (OperatingSystem.IsWindows())
            {
                _writeFileSync = _writeFileSync.Replace("/", "\\");
                _writeFileAsync = _writeFileAsync.Replace("/", "\\");
            }

            var dir = Path.GetDirectoryName(Path.Combine(Environment.CurrentDirectory, _writeFileSync));

            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
        }

        private static (string, IEnumerable<(StructurizerClip clip, Layers layers)> clips) GeneratePuml(
            params Type[] types)
        {
            var clipPuml = string.Empty; // "@startuml\n";
            var clips = new List<(StructurizerClip clip, Layers layers)>();
            foreach (var type in types)
            {
                TypeHolder<StructurizerClip, StructurizerGenerator> typeHolder = new(type);

                Assert.NotNull(typeHolder);
                var clip = typeHolder.Generate(Layers.TypeEnd, true);
                Assert.NotNull(clip);
                clips.Add((clip, Layers.TypeEnd));
            }

            foreach (var type in types)
            {
                TypeHolder<StructurizerClip, StructurizerGenerator> typeHolder = new(type);

                Assert.NotNull(typeHolder);
                var clip = typeHolder.Generate(Layers.Relationships | Layers.Inheritance, true);
                Assert.NotNull(clip);
                clips.Add((clip, Layers.Relationships | Layers.Inheritance));
            }

            foreach (var (clip, layers) in clips)
            {
                var puml = clip.ToString(layers);

                clipPuml += puml;
            }

            //clipPuml += "\n@enduml";

            return (clipPuml.Trim(), clips);
        }
        [Theory]
        [InlineData(typeof(TestClass<>), typeof(Extensions), typeof(TestBase<>), typeof(MyEntity))]
        public void WriteFileSync(params Type[] types)
        {
            if (File.Exists(_writeFileSync)) File.Delete(_writeFileSync);

            var writer = new StructuredWriter<StructurizerClip, StructurizerGenerator>();

            var (clipPuml, clips) = GeneratePuml(types);

            writer.WriteFile(_writeFileSync, clips).Close();

            var puml = File.ReadAllText(_writeFileSync);

            Output.WriteLine(new string('=', 80));
            Output.WriteLine(puml);
            Output.WriteLine(new string('=', 80));
            Output.WriteLine(clipPuml);
            Output.WriteLine(new string('=', 80));

            puml.Should().NotBeNullOrWhiteSpace();
            puml.Should().BeEquivalentTo(clipPuml);
        }

        [Theory]
        [InlineData(typeof(TestClass<>), typeof(Extensions), typeof(TestBase<>), typeof(MyEntity))]
        public async Task WriteFileAsync(params Type[] types)
        {
            Output.WriteLine(_writeFileAsync);
            if (File.Exists(_writeFileAsync)) File.Delete(_writeFileAsync);

            var writer = new StructuredWriter<StructurizerClip, StructurizerGenerator>();

            var (clip, clips) = GeneratePuml(types);

            (await writer.WriteFileAsync(_writeFileAsync, clips)).Close();

            var puml = await File.ReadAllTextAsync(_writeFileAsync);

            Output.WriteLine(new string('=', 80));
            Output.WriteLine(puml);
            Output.WriteLine(new string('=', 80));
            Output.WriteLine(clip);
            Output.WriteLine(new string('=', 80));

            puml.Should().NotBeNullOrWhiteSpace();
            puml.Should().BeEquivalentTo(clip);
        }

        [Theory]
        [InlineData(typeof(TestClass<>), typeof(Extensions), typeof(TestBase<>), typeof(MyEntity))]
        public void WriteStreamSync(params Type[] types)
        {
            var writer = new StructuredWriter<StructurizerClip, StructurizerGenerator>();
            using var stream = new MemoryStream();

            var (clipPuml, clips) = GeneratePuml(types);

            writer.WriteStream(stream, clips);

            stream.Seek(0, SeekOrigin.Begin);

            var buffer = new Span<byte>(new byte[stream.Length]);

            stream.Read(buffer);

            var puml = writer.Encoding.GetString(buffer.ToArray());

            Output.WriteLine(new string('=', 80));
            Output.WriteLine(puml);
            Output.WriteLine(new string('=', 80));
            Output.WriteLine(clipPuml);
            Output.WriteLine(new string('=', 80));

            puml.Should().NotBeNullOrWhiteSpace();
            puml.Should().BeEquivalentTo(clipPuml);
        }

        [Theory]
        [InlineData(typeof(TestClass<>), typeof(Extensions), typeof(TestBase<>), typeof(MyEntity))]
        public async Task WriteStreamAsync(params Type[] types)
        {
            var writer = new StructuredWriter<StructurizerClip, StructurizerGenerator>();
            await using var stream = new MemoryStream();

            var (clipPuml, clips) = GeneratePuml(types);

            await writer.WriteStreamAsync(stream, clips);

            stream.Seek(0, SeekOrigin.Begin);

            var buffer = new Memory<byte>(new byte[stream.Length]);

            await stream.ReadAsync(buffer);

            var puml = writer.Encoding.GetString(buffer.ToArray());

            Output.WriteLine(new string('=', 80));
            Output.WriteLine(puml);
            Output.WriteLine(new string('=', 80));
            Output.WriteLine(clipPuml);
            Output.WriteLine(new string('=', 80));

            puml.Should().NotBeNullOrWhiteSpace();
            puml.Should().BeEquivalentTo(clipPuml);
        }
    }
}
