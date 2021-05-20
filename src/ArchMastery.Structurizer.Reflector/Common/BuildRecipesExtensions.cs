using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ArchMastery.Structurizer.Reflector.Common.Enums;
// ReSharper disable MemberCanBePrivate.Global

namespace ArchMastery.Structurizer.Reflector.Common
{
    public static class BuildRecipesExtensions
    {
        static readonly Regex _regex = new Regex("[^0-9a-zA-Z_]");

        public static string AsSlug(this string source, char neutralCharacter = '_')
            => _regex.Replace(source, neutralCharacter.ToString());

        public static List<FileInfo> WriteAll(this IEnumerable<Assembly> assemblies, DirectoryInfo directory,
                                              WriteStrategy strategy,
                                              Layers layers = Layers.All,
                                              bool showAttributes = false)
        {
            if (!directory.Exists) directory.Create();

            List<(StructurizerClip clip, Layers layers)> clips = new();
            StructuredWriter<StructurizerClip, StructurizerGenerator> writer = new();

            foreach (var assembly in assemblies) clips.AddRange(assembly.BuildAll(layers, showAttributes));

            List<FileInfo> result = new();
            FileStream fs = null;
            switch (strategy)
            {
                case WriteStrategy.OneFile:
                    var oneFileFilename = Path.Combine(directory.FullName, "AllTypes.puml");
                    fs = writer.WriteFile(oneFileFilename, clips);

                    fs.Close();
                    result.Add(new FileInfo(fs.Name));
                    break;

                case WriteStrategy.OneFilePerType:
                    var toProcess = clips
                                   .GroupBy(c => $"{c.clip.Namespace}.{c.clip.TypeName}")
                                   .Select(group => (Path.Combine(directory.FullName, group.Key.AsSlug() + ".puml"),
                                                     group));

                    foreach (var (filename, group) in toProcess)
                    {
                        fs = writer.WriteFile(filename, group);

                        fs.Close();
                        result.Add(new FileInfo(fs.Name));
                    }

                    break;

                case WriteStrategy.OneFilePerNamespace:
                    foreach (var group in clips.GroupBy(c => c.clip.Namespace))
                    {
                        var typeFilename = Path.Combine(directory.FullName, group.Key.AsSlug() + ".puml");
                        fs = writer.WriteFile(typeFilename, clips);
                        result.Add(new FileInfo(fs.Name));
                    }

                    break;

                case WriteStrategy.OneFilePerAssembly:
                    foreach (var group in clips.GroupBy(c => c.clip.Assembly))
                    {
                        var filename = group.Key.GetName().Name!.AsSlug();
                        var typeFilename = Path.Combine(directory.FullName, $"{filename}.puml");
                        fs = writer.WriteFile(typeFilename, group);
                        fs.Close();
                        result.Add(new FileInfo(fs.Name));
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);
            }

            return result;
        }

        public static IEnumerable<(StructurizerClip clip, Layers layers)> BuildAll(this Assembly assembly,
            Layers layers = Layers.All, bool showAttributes = false)
        {
            var types = assembly.GetTypes().Where(t => t.ToString() != "<PrivateImplementationDetails>");
            types = types.Where(t => t.GetCustomAttribute(typeof(CompilerGeneratedAttribute)) is null);

            return BuildTypes(types, layers, showAttributes);
        }

        public static IEnumerable<(StructurizerClip clip, Layers layers)> BuildTypes(IEnumerable<Type> typesEnumerable,
            Layers layers = Layers.All, bool showAttributes = false)
        {
            var enumerable = typesEnumerable.ToArray();
            List<(StructurizerClip clip, Layers layers)> result = new();

            var layer = layers;

            if (layers > Layers.TypeEnd) layer = Layers.TypeEnd;

            while (layers != Layers.None)
            {
                result.AddRange(
                                from type in enumerable
                                select new TypeHolder<StructurizerClip, StructurizerGenerator>(type)
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
