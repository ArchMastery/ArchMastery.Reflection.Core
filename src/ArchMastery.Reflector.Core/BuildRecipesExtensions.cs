using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ArchMastery.Structurizer.Reflector.Common.Base;
using ArchMastery.Structurizer.Reflector.Common.Enums;
using GPS.SimpleThreading.Blocks;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable ParameterTypeCanBeEnumerable.Local

namespace ArchMastery.Structurizer.Reflector.Common
{
    public static class BuildRecipesExtensions
    {
        static readonly Regex _regex = new Regex("[^0-9a-zA-Z_]");

        static readonly ParallelOptions _options = new()
                                                   {
                                                       MaxDegreeOfParallelism = 4
                                                   };


        public static string AsSlug(this string source, char neutralCharacter = '_')
            => _regex.Replace(source, neutralCharacter.ToString());

        public static ConcurrentBag<FileInfo> WriteAll<TClip, TGenerator>(this IEnumerable<Assembly> assemblies,
                                                                          DirectoryInfo directory,
                                                                          WriteStrategy strategy,
                                                                          Layers layers = Layers.All,
                                                                          bool showAttributes = false)
            where TClip : ClipBase, new()
            where TGenerator : GeneratorBase<TClip>, new()
        {
            if (!directory.Exists) directory.Create();

            List<(TClip clip, Layers layers)> clips = new();
            StructuredWriter<TClip, TGenerator> writer = new();

            foreach (var assembly in assemblies)
            {
                clips.AddRange(assembly.BuildAll<TClip, TGenerator>(layers, showAttributes));
            }

            ConcurrentBag<FileInfo> result = new();

            switch (strategy)
            {
                case WriteStrategy.OneFile:
                    WriteStrategyOneFile(directory, writer, clips, result);
                    break;

                case WriteStrategy.OneFilePerType:
                    WriteStrategyOneFilePerType(clips, directory, writer, result);
                    break;

                case WriteStrategy.OneFilePerNamespace:
                    WriteStrategyOneFilePerNamespace(directory, clips, writer, result);
                    break;

                case WriteStrategy.OneFilePerAssembly:
                    WriteStrategyOneFilePerAssembly<TClip, TGenerator>(directory, clips, writer, result);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);
            }

            return result;
        }

        private static void WriteStrategyOneFilePerAssembly<TClip, TGenerator>(DirectoryInfo directoryInfo,
                                                                               List<(TClip clip, Layers layers)>
                                                                                   valueTuples,
                                                                               StructuredWriter<TClip, TGenerator>
                                                                                   structuredWriter,
                                                                               ConcurrentBag<FileInfo> result)
            where TClip : ClipBase, new()
            where TGenerator : GeneratorBase<TClip>, new()
        {
            IEnumerable<IGrouping<Assembly?, (TClip clip, Layers layers)>> forParallel = valueTuples
               .GroupBy(c => c.clip.Assembly);

            ThreadBlock<IGrouping<Assembly?, (TClip clip, Layers layers)>, ConcurrentBag<FileInfo>> block =
                new(Process)
                {
                    MaxDegreeOfParallelism = 4
                };

            block.AddRange(forParallel);
            block.LockList();

            block.Execute(4, null, (task, tuple) =>
                                   {
                                       if (task.IsCompleted && !task.IsFaulted)
                                       {
                                           tuple.Value.result.ToList().ForEach(result.Add);
                                       }
                                       else
                                       {
                                           throw task.Exception;
                                       }
                                   });

            ConcurrentBag<FileInfo> Process(IGrouping<Assembly?, (TClip clip, Layers layers)> group)
            {
                ConcurrentBag<FileInfo> result = new();

                var filename = group.Key.GetName().Name!.AsSlug();
                var typeFilename = Path.Combine(directoryInfo.FullName, $"{filename}.puml");
                var fs = structuredWriter.WriteFile(typeFilename, group);
                fs.Close();
                result.Add(new FileInfo(fs.Name));

                return result;
            }
        }

        private static void WriteStrategyOneFilePerType<TClip, TGenerator>(List<(TClip clip, Layers layers)> clips,
                                                                           DirectoryInfo directory,
                                                                           StructuredWriter<TClip, TGenerator> writer,
                                                                           ConcurrentBag<FileInfo> result)
            where TClip : ClipBase, new()
            where TGenerator : GeneratorBase<TClip>, new()
        {
            var forParallel =
                clips.GroupBy(c => $"{c.clip.TypeName}")
                     .Select(group => (Path.Combine(directory.FullName, @group.Key.AsSlug() + ".puml"), @group));

            ThreadBlock<(string, IGrouping<string, (TClip clip, Layers layers)> @group), ConcurrentBag<FileInfo>> block =
                new(Process)
                {
                    MaxDegreeOfParallelism = 4
                };

            block.AddRange(forParallel);
            block.LockList();

            block.Execute(4, null, (task, tuple) =>
                                   {
                                       if (task.IsCompleted && !task.IsFaulted)
                                       {
                                           tuple.Value.result.ToList().ForEach(result.Add);
                                       }
                                       else
                                       {
                                           throw task.Exception;
                                       }
                                   });

             ConcurrentBag<FileInfo> Process((string, IGrouping<string, (TClip clip, Layers layers)> @group) item)
             {
                 ConcurrentBag<FileInfo> result = new();
                 var (filename, group) = item;
                 var fileStream = writer.WriteFile(filename, @group);

                 fileStream.Close();
                 result.Add(new FileInfo(fileStream.Name));

                 return result;
             }
        }

        private static void WriteStrategyOneFilePerNamespace<TClip, TGenerator>(DirectoryInfo directory,
            List<(TClip clip, Layers layers)> clips,
            StructuredWriter<TClip, TGenerator> writer,
            ConcurrentBag<FileInfo> result)
            where TClip : ClipBase, new()
            where TGenerator : GeneratorBase<TClip>, new()
        {
            var forParallel = clips.GroupBy(c => c.clip.Namespace ?? "<<none>>");

            ThreadBlock<IGrouping<string?, (TClip clip, Layers layers)>, ConcurrentBag<FileInfo>> block = new(Process)
                {
                    MaxDegreeOfParallelism = 4
                };

            block.AddRange(forParallel);
            block.LockList();

            block.Execute(4, null, (task, tuple) =>
                                   {
                                       if (task.IsCompleted && !task.IsFaulted)
                                       {
                                           tuple.Value.result.ToList().ForEach(result.Add);
                                       }
                                       else
                                       {
                                           throw task.Exception;
                                       }
                                   });

            ConcurrentBag<FileInfo> Process(IGrouping<string?, (TClip clip, Layers layers)> grouping)
            {
                ConcurrentBag<FileInfo> result = new();
                if (grouping?.Key is null || directory is null || writer is null) return result;

                var typeFilename = Path.Combine(directory.FullName, grouping.Key.AsSlug() + ".puml");
                var fs = writer.WriteFile(typeFilename, grouping);
                result.Add(new FileInfo(fs.Name));
                return result;
            }
        }

        private static void WriteStrategyOneFile<TClip, TGenerator>(DirectoryInfo directory,
                                                                    StructuredWriter<TClip, TGenerator> writer,
                                                                    List<(TClip clip, Layers layers)> clips,
                                                                    ConcurrentBag<FileInfo> result)
            where TClip : ClipBase, new() where TGenerator : GeneratorBase<TClip>, new()
        {
            var oneFileFilename = Path.Combine(directory.FullName, "AllTypes.puml");
            var fs = writer.WriteFile(oneFileFilename, clips);
            fs.Close();
            result.Add(new FileInfo(fs.Name));
        }

        public static IEnumerable<(TClip clip, Layers layers)> BuildAll<TClip, TGenerator>(this Assembly assembly,
            Layers layers = Layers.All, bool showAttributes = false)
            where TClip : ClipBase, new()
            where TGenerator : GeneratorBase<TClip>, new()
        {
            var types = assembly.GetTypes().Where(t => t.ToString() != "<PrivateImplementationDetails>");
            types = types.Where(t => t.GetCustomAttribute(typeof(CompilerGeneratedAttribute)) is null);

            return BuildTypes<TClip, TGenerator>(types, layers, showAttributes);
        }

        public static IEnumerable<(TClip clip, Layers layers)> BuildTypes<TClip, TGenerator>(
            IEnumerable<Type> typesEnumerable,
            Layers layers = Layers.All, bool showAttributes = false)
            where TClip : ClipBase, new()
            where TGenerator : GeneratorBase<TClip>, new()
        {
            var enumerable = typesEnumerable.ToArray();
            List<(TClip clip, Layers layers)> result = new();

            var layer = layers;

            if (layers > Layers.TypeEnd) layer = Layers.TypeEnd;

            while (layers != Layers.None)
            {
                result.AddRange(
                                from type in enumerable
                                select new TypeHolder<TClip, TGenerator>(type)
                                into holder
                                select holder.Generate(layers, showAttributes)
                                into clip
                                select (clip, layer));

                if (layers > Layers.TypeEnd)
                {
                    layer = layers;
                    layers = layers switch
                             {
                                 Layers.Relationships => Layers.Relationships,
                                 Layers.Inheritance => Layers.Inheritance,
                                 Layers.All => Layers.Inheritance | Layers.Relationships | Layers.Notes,
                                 _ => Layers.None
                             };
                }
                else
                {
                    layers = Layers.None;
                }
            }

            return result;
        }
    }
}
