#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ArchMastery.Structurizer.Reflector.Common.Enums;

// ReSharper disable MemberCanBeProtected.Global

namespace ArchMastery.Structurizer.Reflector.Common.Base
{
    public abstract class GeneratorBase<TClip>
        where TClip : ClipBase, new()
    {
        private string? _fullName;

        public IEnumerable<string> Exclusions { get; set; } =
            new[] {"System.", "Windows.", "Microsoft."};

        public Type? ObjectType { get; init; }

        public string? Slug => ObjectType?.NormalizeName().AsSlug();
        public string? DisplayName => ObjectType?.Name.NormalizeNameString();
        public string TypeFullName => _fullName ??= MakeFullName();

        private string MakeFullName()
        {
            return ObjectType?.FullName.NormalizeNameString()
                ?? $"{ObjectType?.Namespace}.{DisplayName}";
        }

        protected string GetObjectType()
        {
            var typeMap =
                ((ObjectType?.IsClass ?? false) && !(ObjectType?.IsAbstract ?? false),
                 (ObjectType?.IsClass ?? false) && (ObjectType?.IsAbstract ?? false),
                 ObjectType?.IsEnum, ObjectType?.IsArray, ObjectType?.IsInterface,
                 ObjectType?.IsValueType);

            var objectType = typeMap switch
                             {
                                 (true, _, _, _, _, _) => "class",
                                 (_, true, _, _, _, _) => "abstract class",
                                 (_, _, true, _, _, _) => "enum",
                                 (_, _, _, _, true, _) => "interface",
                                 _ => "entity"
                             };

            return objectType;
        }

        public abstract string StartType(TypeInfo type, bool showAttributes, string attributes);
        public abstract string EndType(TypeInfo type);
        public abstract string GetAttributes(ICollection<CustomAttributeData> attributes, bool showAttributes);
        public abstract string GetExtends(string innerBaseName);
        public abstract string GetImplements(TypeInfo inheritsTypeInfo);
        public abstract string GetRelationships(FieldInfo field, string? fieldTypeName, string? arrayElementTypeName,
                                                Type? genericCollectionType, string? genericCollectionTypeName);
        public abstract string GetRelationships(PropertyInfo property, string? propertyTypeName,
                                                string? arrayElementTypeName, Type? genericCollectionType,
                                                string? genericCollectionTypeName);
        public abstract string GetMember(FieldInfo fieldInfo, string attributes);
        public abstract string GetMember(PropertyInfo propertyInfo, string attributes);
        public abstract string GetMember(MethodInfo methodInfo, string methodName, string attributes);
        public abstract string GetMember(ConstructorInfo ctorInfo, string attributes);
        public abstract string GetMember(EventInfo eventInfo, string attributes);

        public abstract string CreatDivider(char dividerChar, MemberTypes memberType);

        public abstract string MakeParList(IEnumerable<ParameterInfo> parameters);

        public abstract string GetAccessibility(bool isStatic, bool isAbstract, bool isPublic,
                                                bool isPrivate, bool isFamily, bool isAssembly);

        public abstract string GetAccessibility(bool methodIsPublic, bool methodIsPrivate, bool methodIsFamily,
                                                bool isAssembly);

        public Type? GetArrayType<TGenerator>(Type arrayType)
            where TGenerator : GeneratorBase<TClip>, new()
        {
            if (!arrayType.IsArray) return null;

            var trimmedName = arrayType.Name.TrimEnd("[]".ToCharArray());
            var types =
                GetNestedTypes<TGenerator>()
                   .Where(nt => nt.ObjectType.Name == trimmedName);

            return types.FirstOrDefault()?.ObjectType ?? GetFromAssembly();

            Type GetFromAssembly()
            {
                return arrayType.Assembly
                                .GetTypes()
                                .FirstOrDefault(t => t.Name == trimmedName)
                    ?? arrayType;
            }
        }

        public IEnumerable<TypeHolder<TClip, TGenerator>> GetNestedTypes<TGenerator>()
            where TGenerator : GeneratorBase<TClip>, new()
            => ObjectType?.GetNestedTypes().Select(nestedType => new TypeHolder<TClip, TGenerator>(nestedType))
                ?? Array.Empty<TypeHolder<TClip, TGenerator>>();

        public virtual void BuildDocument(Stream stream, IEnumerable<(TClip clip, Layers layer)> clips)
        {
            var bytes = BuildDocumentString(clips);
            stream.Write(bytes, 0, bytes.Length);
        }

        public virtual async Task BuildDocumentAsync(Stream stream, IEnumerable<(TClip clip, Layers layer)> clips)
        {
            var bytes = BuildDocumentString(clips);
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }

        protected virtual byte[] BuildDocumentString(IEnumerable<(TClip clip, Layers layer)> clips)
        {
            var segments =
                clips.Where(p => p.layer <= Layers.TypeEnd)
                     .Select(pair => (pair.clip, Layers.TypeEnd))
                     .ToList();

            segments.AddRange(clips.Where(p => p.layer > Layers.TypeEnd)
                                   .Select(pair => (pair.clip, Layers.Relationships | Layers.Inheritance)));


            var sb = new StringBuilder();

            foreach (var (clip, layers) in segments)
            {
                var puml = clip.ToString(layers).Trim();

                sb.AppendLine(puml);
            }

            return Encoding.UTF8.GetBytes(sb.ToString().Trim());
        }
    }
}
