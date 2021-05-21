using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ArchMastery.Reflector.Core;
using ArchMastery.Reflector.Core.Enums;
using ArchMastery.Reflector.Structurizer;
using Divergic.Logging.Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace ArchMastery.Structurizer.Reflector.xUnit
{
    public class RecipeTests : LoggingTestsBase
    {
        private const string AssemblyPath1 = "./TestAssemblies/FluentAssertions.dll";
        private const string AssemblyPath2 = "./TestAssemblies/Newtonsoft.Json.dll";

        private static readonly string[] Assemblies =
        {
            AssemblyPath1, AssemblyPath2
        };

        public RecipeTests(ITestOutputHelper output) : base(output, LogLevel.Debug)
        {
        }

        [Theory]
        [InlineData(AssemblyPath1, Layers.All, true)]
        [InlineData(AssemblyPath2, Layers.All, true)]
        public void WriteDocumentPerType(string assemblyPath, Layers layers, bool includeAttributes)
        {
            if (OperatingSystem.IsWindows()) assemblyPath = assemblyPath.Replace("/", "\\");
            var path = Path.Combine(Environment.CurrentDirectory, assemblyPath);
            var assembly = Assembly.LoadFile(path);

            assembly.Should().NotBeNull();

            var directoryInfo = new DirectoryInfo($"{Path.GetFileNameWithoutExtension(path)}\\PerType");

            if (directoryInfo.Exists && directoryInfo.GetFiles().Length > 0)
                directoryInfo.GetFiles().Select(f => f.FullName).ToList().ForEach(File.Delete);

            var types = assembly.GetTypes().Where(t => t.ToString() != "<PrivateImplementationDetails>");
            types = types.Where(t => t.GetCustomAttribute(typeof(CompilerGeneratedAttribute)) is null);

            var grouped = types
                         .GroupBy(c => $"{c.NormalizeName()!.AsSlug()}")
                         .Select(group =>
                                     (Path.Combine(directoryInfo.FullName, group.Key.AsSlug() + ".puml"), group))
                         .Select(pair => pair.group.Key)
                         .ToList();

            var result = new[] {assembly}.WriteAll<StructurizerClip, StructurizerGenerator>(
                                                   directoryInfo,
                                                   WriteStrategy.OneFilePerType,
                                                   layers,
                                                   includeAttributes);

            result.Should().NotBeNullOrEmpty();

            if (grouped.Count != result.Count)
            {
                Output.WriteLine($"grouped.Count: {grouped.Count}");
                Output.WriteLine($"result.Count: {result.Count}");

                grouped.ForEach(g =>
                                {
                                    var key = g.AsSlug();
                                    var isNotFound = result.All(r => Path.GetFileNameWithoutExtension(r.Name) != key);

                                    if (isNotFound)
                                    {
                                        Output.WriteLine($"{key} is not in results.");
                                    }
                                });

                result.Select(r => Path.GetFileNameWithoutExtension(r.Name)).ToList().ForEach(r =>
                {
                    var key = r.AsSlug();
                    var isNotFound = grouped.All(g => g.AsSlug() != key);

                    if (isNotFound)
                    {
                        Output.WriteLine($"{key} is not in grouped.");
                    }
                });

                result.Count.Should().Be(grouped.Count);
            }


            var file = result.First();

            file.Should().NotBeNull();
            File.Exists(file.FullName).Should().BeTrue();
            Output.WriteLine(file.FullName);
        }

        [Theory]
        [InlineData(AssemblyPath1, Layers.All, true)]
        [InlineData(AssemblyPath2, Layers.All, true)]
        public void WriteDocumentPerNamespace(string assemblyPath, Layers layers, bool includeAttributes)
        {
            if (OperatingSystem.IsWindows()) assemblyPath = assemblyPath.Replace("/", "\\");
            var path = Path.Combine(Environment.CurrentDirectory, assemblyPath);
            var assembly = Assembly.LoadFile(path);

            assembly.Should().NotBeNull();

            var directoryInfo = new DirectoryInfo($"{Path.GetFileNameWithoutExtension(path)}\\PerNamespace");

            if (directoryInfo.Exists && directoryInfo.GetFiles().Length > 0)
                directoryInfo.GetFiles().Select(f => f.FullName).ToList().ForEach(File.Delete);

            var types = assembly.GetTypes().Where(t => t.ToString() != "<PrivateImplementationDetails>");
            types = types.Where(t => t.GetCustomAttribute(typeof(CompilerGeneratedAttribute)) is null);
            var namespaces = types.GroupBy(t => t.Namespace);

            var result = new[] {assembly}.WriteAll<StructurizerClip, StructurizerGenerator>(
                                                   directoryInfo,
                                                   WriteStrategy.OneFilePerNamespace,
                                                   layers,
                                                   includeAttributes);

            result.Should().NotBeNullOrEmpty();
            result.Count.Should().Be(namespaces.Count());

            var file = result.First();

            file.Should().NotBeNull();
            File.Exists(file.FullName).Should().BeTrue();
            Output.WriteLine(file.FullName);
        }

        [Theory]
        [InlineData(AssemblyPath1, Layers.All, true)]
        [InlineData(AssemblyPath2, Layers.All, true)]
        public void WriteDocumentPerAssembly(string assemblyPath, Layers layers, bool includeAttributes)
        {
            if (OperatingSystem.IsWindows()) assemblyPath = assemblyPath.Replace("/", "\\");
            var path = Path.Combine(Environment.CurrentDirectory, assemblyPath);
            var assembly = Assembly.LoadFile(path);

            assembly.Should().NotBeNull();

            var directoryInfo = new DirectoryInfo($"{Path.GetFileNameWithoutExtension(path)}\\PerAssembly");

            if (directoryInfo.Exists && directoryInfo.GetFiles().Length > 0)
                directoryInfo.GetFiles().Select(f => f.FullName).ToList().ForEach(File.Delete);

            var result = new[] {assembly}.WriteAll<StructurizerClip, StructurizerGenerator>(
                                                   directoryInfo,
                                                   WriteStrategy.OneFilePerAssembly,
                                                   layers,
                                                   includeAttributes);

            result.Should().NotBeNullOrEmpty();
            result.Count.Should().Be(1);

            var file = result.First();

            file.Should().NotBeNull();
            File.Exists(file.FullName).Should().BeTrue();
            Output.WriteLine(file.FullName);
        }

        [Theory]
        [InlineData(AssemblyPath1, Layers.All, true)]
        [InlineData(AssemblyPath2, Layers.All, true)]
        public void WriteFilePerType(string assemblyPath, Layers layers, bool includeAttributes)
        {
            if (OperatingSystem.IsWindows()) assemblyPath = assemblyPath.Replace("/", "\\");
            var path = Path.Combine(Environment.CurrentDirectory, assemblyPath);
            var assembly = Assembly.LoadFile(path);

            assembly.Should().NotBeNull();

            var directoryInfo = new DirectoryInfo($"{Path.GetFileNameWithoutExtension(path)}\\PerType");

            if (directoryInfo.Exists && directoryInfo.GetFiles().Length > 0)
                directoryInfo.GetFiles().Select(f => f.FullName).ToList().ForEach(File.Delete);

            var types =
                assembly
                   .GetTypes()
                   .Where(t => t.ToString() != "<PrivateImplementationDetails>" &&
                               t.GetCustomAttribute(typeof(CompilerGeneratedAttribute)) is null)
                   .ToList();

            var grouped = types
                         .GroupBy(c => $"{c.NormalizeName()!.AsSlug()}")
                         .Select(group =>
                                     (Path.Combine(directoryInfo.FullName, group.Key.AsSlug() + ".puml"), group))
                         .Select(pair => pair.group.Key)
                         .ToList();


            var result = new[] {assembly}.WriteAll<StructurizerClip, StructurizerGenerator>(
                                                   directoryInfo,
                                                   WriteStrategy.OneFilePerType,
                                                   layers,
                                                   includeAttributes);

            result.Should().NotBeNullOrEmpty();

            var slugs = types.Select(type => new { Type = type, Slug = type.NormalizeName()!.AsSlug()}).ToList();

            List<(TypeHolder<StructurizerClip, StructurizerGenerator>, string)> duplicates = new();

            try
            {
                duplicates.AddRange(slugs.Where(slug => slugs.Count(ss => ss.Slug.Equals(slug)) > 1)
                                              .Select(pair =>
                                                          (new TypeHolder<StructurizerClip, StructurizerGenerator>(pair.Type),
                                                           pair.Slug)));

                duplicates.Should().BeEmpty();
            }
            catch (InvalidOperationException)
            {
                duplicates.ForEach(name =>
                                   {
                                       var clip = name.Item1.Generate(Layers.TypeEnd);
                                       Output.WriteLine(new string('-', 80));
                                       Output.WriteLine(clip.ToString(Layers.TypeEnd));
                                   });

                duplicates.Count.Should().Be(0);
            }


            if (grouped.Count != result.Count)
            {
                Output.WriteLine($"grouped.Count: {grouped.Count}");
                Output.WriteLine($"result.Count: {result.Count}");

                grouped.ForEach(g =>
                                {
                                    var key = g.AsSlug();
                                    var isNotFound = result.All(r => Path.GetFileNameWithoutExtension(r.Name) != key);

                                    if (isNotFound)
                                    {
                                        Output.WriteLine($"{key} is not in results.");
                                    }
                                });

                result.Select(r => Path.GetFileNameWithoutExtension(r.Name)).ToList().ForEach(r =>
                {
                    var key = r.AsSlug();
                    var isNotFound = grouped.All(g => g.AsSlug() != key);

                    if (isNotFound)
                    {
                        Output.WriteLine($"{key} is not in grouped.");
                    }
                });

                result.Count.Should().Be(grouped.Count);
            }

            var file = result.First();

            file.Should().NotBeNull();
            File.Exists(file.FullName).Should().BeTrue();
            Output.WriteLine(file.FullName);
        }

        [Theory]
        [InlineData(AssemblyPath1, Layers.All, true)]
        [InlineData(AssemblyPath2, Layers.All, true)]
        public void WriteFilePerNamespace(string assemblyPath, Layers layers, bool includeAttributes)
        {
            if (OperatingSystem.IsWindows()) assemblyPath = assemblyPath.Replace("/", "\\");
            var path = Path.Combine(Environment.CurrentDirectory, assemblyPath);
            var assembly = Assembly.LoadFile(path);

            assembly.Should().NotBeNull();

            var directoryInfo = new DirectoryInfo($"{Path.GetFileNameWithoutExtension(path)}\\PerNamespace");

            if (directoryInfo.Exists && directoryInfo.GetFiles().Length > 0)
                directoryInfo.GetFiles().Select(f => f.FullName).ToList().ForEach(File.Delete);

            var types = assembly.GetTypes().Where(t => t.ToString() != "<PrivateImplementationDetails>");
            types = types.Where(t => t.GetCustomAttribute(typeof(CompilerGeneratedAttribute)) is null);
            var namespaces = types.GroupBy(t => t.Namespace);

            var result = new[] {assembly}.WriteAll<StructurizerClip, StructurizerGenerator>(
                                                   directoryInfo,
                                                   WriteStrategy.OneFilePerNamespace,
                                                   layers,
                                                   includeAttributes);

            result.Should().NotBeNullOrEmpty();
            result.Count.Should().Be(namespaces.Count());

            var file = result.First();

            file.Should().NotBeNull();
            File.Exists(file.FullName).Should().BeTrue();
            Output.WriteLine(file.FullName);
        }

        [Theory]
        [InlineData(Layers.All, true)]
        public void WriteFilePerAssembly(Layers layers, bool includeAttributes)
        {
            var assemblyPaths = Assemblies;
            if (OperatingSystem.IsWindows()) assemblyPaths = assemblyPaths.Select(a => a.Replace("/", "\\")).ToArray();
            var paths = assemblyPaths.Select(a => Path.Combine(Environment.CurrentDirectory, a)).ToList();
            var assemblies = new List<Assembly>();
            DirectoryInfo? directoryInfo = null;
            paths.ForEach(path =>
                          {
                              var assembly = Assembly.LoadFile(path);

                              assembly.Should().NotBeNull();

                              assemblies.Add(assembly);

                              directoryInfo ??=
                                  new DirectoryInfo($"{Path.GetFileNameWithoutExtension(path)}\\PerAssembly");

                              if (directoryInfo.Exists && directoryInfo.GetFiles().Length > 0)
                                  directoryInfo.GetFiles().Select(f => f.FullName).ToList().ForEach(File.Delete);
                          });

            var result = assemblies.WriteAll<StructurizerClip, StructurizerGenerator>(
                                                   directoryInfo!,
                                                   WriteStrategy.OneFilePerAssembly,
                                                   layers,
                                                   includeAttributes);

            result.Should().NotBeNullOrEmpty();
            result.Count.Should().Be(Assemblies.Length);

            var file = result.First();

            file.Should().NotBeNull();
            File.Exists(file.FullName).Should().BeTrue();
            Output.WriteLine(file.FullName);
        }

        [Theory]
        [InlineData(AssemblyPath1, Layers.All, true)]
        [InlineData(AssemblyPath2, Layers.All, true)]
        public void AllInAssembly(string assemblyPath, Layers layers, bool includeAttributes)
        {
            var path = Path.Combine(Environment.CurrentDirectory, assemblyPath);
            var assembly = Assembly.LoadFile(path);

            assembly.Should().NotBeNull();

            var results = assembly.BuildAll<StructurizerClip, StructurizerGenerator>(layers, includeAttributes).ToList();

            results.Should().NotBeNullOrEmpty();

            var types = assembly.GetTypes().Where(t => t.ToString() != "<PrivateImplementationDetails>").ToList();
            types = types.Where(t => t.GetCustomAttribute(typeof(CompilerGeneratedAttribute)) is null).ToList();

            var grouped = types
                         .GroupBy(c => $"{c.NormalizeName()!.AsSlug()}")
                         .ToList();

            var result = results.GroupBy(i => i.clip.Namespace + "." + i.clip.TypeName).ToList();

            if (grouped.Count != result.Count)
            {
                Output.WriteLine($"grouped.Count: {grouped.Count}");
                Output.WriteLine($"result.Count: {result.Count}");

                grouped.ForEach(g =>
                                {
                                    var key = g.Key;
                                    var isNotFound = result.All(r => Path.GetFileNameWithoutExtension(r.Key) != key);

                                    if (isNotFound)
                                    {
                                        Output.WriteLine($"{key} is not in results.");
                                    }
                                });

                result.Select(r => Path.GetFileNameWithoutExtension(r.Key)).ToList().ForEach(r =>
                {
                    var key = r.AsSlug();
                    var isNotFound = grouped.All(g => g.Key != key);

                    if (isNotFound)
                    {
                        Output.WriteLine($"{key} is not in grouped.");
                    }
                });

                result.Count.Should().Be(grouped.Count);
            }

            results.GroupBy(r => r.layers).Count().Should().Be(2);

            var groupedTypes = grouped.Where(g => g.Count() > 1).ToList();

            if (groupedTypes.Count > 0)
            {
                foreach (var grouping in groupedTypes)
                {
                    foreach (var type in grouping)
                    {
                        var typeHolder = new TypeHolder<StructurizerClip, StructurizerGenerator>(type);
                        Output.WriteLine(typeHolder.Generate(Layers.TypeEnd).ToString(Layers.TypeEnd));
                    }
                }

                groupedTypes.Count.Should().Be(0);
            }

            // Output.WriteLine("@startuml");
            results.ToList().ForEach(r => Output.WriteLine(r.clip.ToString(r.layers)));
            // Output.WriteLine("@enduml");
        }
    }
}
