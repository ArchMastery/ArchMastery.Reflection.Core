using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ArchMastery.Structurizer.Reflector.Common.Base;
using ArchMastery.Structurizer.Reflector.Common.Enums;
using MemberTypes = ArchMastery.Structurizer.Reflector.Common.Base.MemberTypes;
using static ArchMastery.Structurizer.Reflector.TypeExtensions;
// ReSharper disable MemberCanBePrivate.Global

// ReSharper disable UnusedMember.Global

#nullable enable
namespace ArchMastery.Structurizer.Reflector.Common
{
    public class TypeHolder<TClip, TGenerator>
        where TClip : ClipBase, new()
        where TGenerator : GeneratorBase<TClip>, new()
    {
        private readonly List<IMemberHolder> _members;
        public TGenerator Generator { get; }

        public TypeHolder(Type objectType)
        {
            ObjectType = objectType;
            _members = new List<IMemberHolder>();
            Generator = new TGenerator
                         {
                             ObjectType = objectType
                         };
        }

        public Type ObjectType { get; }

        public IEnumerable<IMemberHolder> Members => _members;


        public TClip Generate(Layers layers, bool showAttributes = false)
        {
            var result = new TClip
                         {
                             TypeName = Generator.Slug ?? string.Empty,
                             Namespace = ObjectType.Namespace,
                             Assembly = ObjectType.Assembly
                         };

            var version = result.Version;

            var layerMap = (
                               layers & Layers.Type,
                               layers & Layers.Inheritance,
                               layers & Layers.NonPublic,
                               layers & Layers.Public,
                               layers & Layers.Relationships,
                               layers & Layers.Notes
                           );

            GetObject(layers, layerMap, result, version, showAttributes);

            GetInheritance(layerMap, result);

            GetRelationships(layerMap, result);

            GetNotes(layerMap, result);

            return result;
        }

        private void GetInheritance(
            (Layers, Layers, Layers, Layers, Layers, Layers) layerMap,
            ClipBase result)
        {
            if (layerMap is not (_, Layers.Inheritance, _, _, _, _)) return;

            var baseName = (ObjectType.BaseType ?? typeof(object)).NormalizeName();

            var inheritanceSegment = ObjectType.BaseType?.Name switch
                                     {
                                         "Object" => string.Empty,
                                         null => string.Empty,
                                         _ => Generator.GetExtends(ObjectType.BaseType.FullName ??
                                                         $"{ObjectType.BaseType.Namespace}.{baseName}")
                                     };

            var member = CreateMemberHolder(ObjectType.GetTypeInfo(),
                                            (Layers.Inheritance, inheritanceSegment, MemberTypes.Extends));

            result.Segments.Add(member);


            ObjectType.GetInterfaces().OrderBy(i => i.FullName).ToList().ForEach(interfaceType =>
            {
                var normalizedName = interfaceType.NormalizeName();
                var interfaceName =
                    (interfaceType.FullName ?? $"{interfaceType.Namespace}.{normalizedName}").NormalizeNameString();

                if (Generator.Exclusions.Any(e => interfaceName!.StartsWith(e))) return;

                var innerSegment = interfaceType.Name switch
                                   {
                                       "INullable" => string.Empty,
                                       _ => Generator.GetImplements(interfaceType.GetTypeInfo())
                                   };

                var innerMember =
                    CreateMemberHolder(ObjectType.GetTypeInfo(),
                                       (Layers.Inheritance, innerSegment, MemberTypes.Implements));

                result.Segments.Add(innerMember);
            });
        }

        private void GetObject(Layers layers,
                               (Layers, Layers, Layers, Layers, Layers, Layers) layerMap,
                               TClip result,
                               // ReSharper disable once UnusedParameter.Local
                               int version, bool showAttributes)
        {
            if (layerMap is (Layers.Type, _, _, _, _, _))
            {
                var attributes = Generator.GetAttributes(ObjectType.GetCustomAttributesData(), showAttributes);

                var segment = Generator.StartType(ObjectType.GetTypeInfo(), showAttributes, attributes);

                var member = CreateMemberHolder(ObjectType.GetTypeInfo(), (Layers.Type, segment, MemberTypes.None));

                result.Segments.Add(member);
            }

            GetMembers(layerMap, result, showAttributes);

            if (layerMap is not (Layers.Type, _, _, _, _, _)) return;

            var endSegment = Generator.EndType(ObjectType.GetTypeInfo());

            var endMemberInfo = CreateMemberHolder(ObjectType.GetTypeInfo(), (Layers.TypeEnd, endSegment, MemberTypes.None));

            result.Segments.Add(endMemberInfo);

            foreach (var nestedType in Generator.GetNestedTypes<TGenerator>())
            {
                var nestedMember = CreateMemberHolder(nestedType.ObjectType.GetTypeInfo(),
                                                      (Layers.InnerObjects,
                                                       nestedType.Generate(layers).ToString(layers),
                                                       MemberTypes.InnerType));

                result.Segments.Add(nestedMember);
            }
        }

        public IMemberHolder CreateMemberHolder<TInfo>(
            TInfo info, (Layers layer, string segment, MemberTypes memberType) segment)
            where TInfo : class
        {
            var member = new MemberHolder<TInfo>(info, segment);
            _members.Add(member);
            return member;
        }

        private void GetMembers((Layers, Layers, Layers, Layers, Layers, Layers) layerMap, TClip result,
                                bool showAttributes)
        {
            var privateFields = ObjectType.GetRuntimeFields().Where(fi => fi.IsPrivate).ToList();
            var fields = ObjectType.GetRuntimeFields().Where(fi => fi.IsPublic).ToList();

            if (layerMap is (_, _, Layers.NonPublic, _, _, _) && privateFields.Any() ||
                layerMap is (_, _, _, Layers.Public, _, _) && fields.Any())
            {
                var memberHolder = CreateMemberHolder(privateFields.FirstOrDefault() ?? fields.First(),
                                                      (Layers.Members, Generator.CreatDivider('.', MemberTypes.Field), MemberTypes.None));
                result.Segments.Add(memberHolder);
            }

            if (layerMap is (_, _, Layers.NonPublic, _, _, _))
                foreach (var member in privateFields)
                {
                    var memberHolder = CreateMemberHolder(member,
                                                          (Layers.Members, string.Join("\n\t",
                                                            BuildField(member,
                                                                       showAttributes)), MemberTypes.Field));
                    result.Segments.Add(memberHolder);
                }

            if (layerMap is (_, _, _, Layers.Public, _, _))
                foreach (var member in fields)
                {
                    var memberHolder = CreateMemberHolder(member,
                                                          (Layers.Members, string.Join("\n\t",
                                                            BuildField(member,
                                                                       showAttributes)), MemberTypes.Field));
                    result.Segments.Add(memberHolder);
                }

            var privateCtors = BuildConstructors(Layers.NonPublic, showAttributes).ToList()!;
            var ctors = BuildConstructors(Layers.Public, showAttributes).ToList()!;

            var allCtors = new List<(ConstructorInfo ctor, string segment)>(privateCtors);
            allCtors.AddRange(ctors);

            if (layerMap is (_, _, Layers.NonPublic, _, _, _) && privateCtors.Any() ||
                layerMap is (_, _, _, Layers.Public, _, _) && ctors.Any())
            {
                var memberHolder = CreateMemberHolder(allCtors.First().ctor,
                                                      (Layers.Members, Generator.CreatDivider('.', MemberTypes.Ctor), MemberTypes.None));
                result.Segments.Add(memberHolder);
            }

            if (layerMap is (_, _, Layers.NonPublic, _, _, _))
                privateCtors.ForEach(pair =>
                                     {
                                         var (ctor, segment) = pair;
                                         var memberHolder = CreateMemberHolder(ctor,
                                                                               (Layers.Members, segment,
                                                                                       MemberTypes.Ctor));
                                         result.Segments.Add(memberHolder);
                                     });

            if (layerMap is (_, _, _, Layers.Public, _, _))
                ctors.ForEach(pair =>
                              {
                                  var (ctor, segment) = pair;
                                  var memberHolder = CreateMemberHolder(ctor,
                                                                        (Layers.Members, segment, MemberTypes.Ctor));
                                  result.Segments.Add(memberHolder);
                              });

            var privateProperties =
                ObjectType.GetRuntimeProperties().Where(fi => fi.GetMethod?.IsPrivate ?? true).ToList();
            var properties = ObjectType.GetRuntimeProperties().Where(fi => fi.GetMethod?.IsPublic ?? false).ToList();

            if (layerMap is (_, _, Layers.NonPublic, _, _, _) && privateProperties.Any() ||
                layerMap is (_, _, _, Layers.Public, _, _) && properties.Any())
            {
                var memberHolder = CreateMemberHolder(privateProperties.FirstOrDefault() ?? properties.First(),
                                                      (Layers.Members, Generator.CreatDivider('.', MemberTypes.Property), MemberTypes.None));
                result.Segments.Add(memberHolder);
            }

            if (layerMap is (_, _, Layers.NonPublic, _, _, _))
                foreach (var member in privateProperties)
                {
                    var memberHolder = CreateMemberHolder(member,
                                                          (Layers.Members, string.Join("\n\t",
                                                            BuildProperty(member,
                                                                          showAttributes)), MemberTypes.Property));
                    result.Segments.Add(memberHolder);
                }

            if (layerMap is (_, _, _, Layers.Public, _, _))
                foreach (var member in properties)
                {
                    var memberHolder = CreateMemberHolder(member,
                                                          (Layers.Members, string.Join("\n\t",
                                                            BuildProperty(member,
                                                                          showAttributes)), MemberTypes.Property));
                    result.Segments.Add(memberHolder);
                }

            var privateMethods = ObjectType.GetRuntimeMethods().Where(fi =>
                                                                          fi.IsPrivate &&
                                                                          !fi.Name.StartsWith("get_", true,
                                                                           CultureInfo.CurrentCulture) &&
                                                                          !fi.Name.StartsWith("set_", true,
                                                                           CultureInfo.CurrentCulture)).ToList();
            var methods = ObjectType.GetRuntimeMethods().Where(fi =>
                                                                   fi.IsPublic &&
                                                                   !fi.Name.StartsWith("get_", true,
                                                                    CultureInfo.CurrentCulture) &&
                                                                   !fi.Name.StartsWith("set_", true,
                                                                    CultureInfo.CurrentCulture)).ToList();

            if (layerMap is (_, _, Layers.NonPublic, _, _, _) && privateMethods.Any() ||
                layerMap is (_, _, _, Layers.Public, _, _) && methods.Any())
            {
                var memberHolder = CreateMemberHolder(privateMethods.FirstOrDefault() ?? methods.First(),
                                                      (Layers.Members, Generator.CreatDivider('.', MemberTypes.Method), MemberTypes.None));
                result.Segments.Add(memberHolder);
            }

            if (layerMap is (_, _, Layers.NonPublic, _, _, _))
                foreach (var member in privateMethods)
                {
                    var memberHolder = CreateMemberHolder(member,
                                                          (Layers.Members, string.Join("\n\t",
                                                            BuildMethod(member,
                                                                        showAttributes)), MemberTypes.Method));
                    result.Segments.Add(memberHolder);
                }

            if (layerMap is (_, _, _, Layers.Public, _, _))
                foreach (var member in methods)
                {
                    var memberHolder = CreateMemberHolder(member,
                                                          (Layers.Members, string.Join("\n\t",
                                                            BuildMethod(member,
                                                                        showAttributes)), MemberTypes.Method));
                    result.Segments.Add(memberHolder);
                }

            var privateEvents = ObjectType.GetRuntimeEvents().Where(fi => fi.AddMethod?.IsPrivate ?? true).ToList();
            var events = ObjectType.GetRuntimeEvents().Where(fi => fi.AddMethod?.IsPublic ?? false).ToList();

            if (layerMap is (_, _, Layers.NonPublic, _, _, _) && privateEvents.Any() ||
                layerMap is (_, _, _, Layers.Public, _, _) && events.Any())
            {
                var memberHolder = CreateMemberHolder(privateEvents.FirstOrDefault() ?? events.First(),
                                                      (Layers.Members, Generator.CreatDivider('.', MemberTypes.Event), MemberTypes.None));
                result.Segments.Add(memberHolder);
            }

            if (layerMap is (_, _, Layers.NonPublic, _, _, _))
                foreach (var member in privateEvents)
                {
                    var memberHolder = CreateMemberHolder(member,
                                                          (Layers.Members, string.Join("\n\t",
                                                            BuildEvent(member,
                                                                       showAttributes)), MemberTypes.Attribute));
                    result.Segments.Add(memberHolder);
                }

            if (layerMap is not (_, _, _, Layers.Public, _, _)) return;

            foreach (var member in events)
            {
                var memberHolder = CreateMemberHolder(member,
                                                      (Layers.Members, string.Join("\n\t",
                                                        BuildEvent(member,
                                                                   showAttributes)), MemberTypes.Event));
                result.Segments.Add(memberHolder);
            }
        }

        // ReSharper disable once UnusedParameter.Local
        private static void GetNotes((Layers, Layers, Layers, Layers, Layers, Layers) layerMap, TClip result)
        {
            if (layerMap is (_, _, _, _, _, Layers.Notes))
            {
            }
        }

        private void GetRelationships((Layers, Layers, Layers, Layers, Layers, Layers) layerMap,
                                      TClip result)
        {
            if (layerMap is not (_, _, _, _, Layers.Relationships, _)) return;
            if (ObjectType.IsEnum) return;

            var mapped = new List<string>();

            foreach (var field in ObjectType.GetRuntimeFields())
            {
                var isSpecialName = (field.Attributes & FieldAttributes.SpecialName) == FieldAttributes.SpecialName;
                if (isSpecialName) continue;
                if (field.Name.EndsWith("_BackingField")) continue;
                if (field.DeclaringType.NormalizeName() != ObjectType.NormalizeName()) continue;
                if (field.FieldType.Name.EndsWith(nameof(EventHandler))) continue;
                if (field.FieldType.Name != field.FieldType.NormalizeType()) continue;

                var fieldTypeName =
                    field.FieldType.IsGenericType
                        ? (field.FieldType.GetGenericArguments().First()).NormalizeName()
                        : (field.FieldType).NormalizeName();

                var genericCollectionType = field.FieldType.Name.StartsWith("IEnumerable")
                                                ? field.FieldType.GetGenericArguments().First()
                                                : typeof(object);

                var genericCollectionTypeName = genericCollectionType.NormalizeName();

                var arrayType = Generator.GetArrayType<TGenerator>(field.FieldType);
                var arrayElementType = arrayType.NormalizeName();

                if (arrayElementType is null)
                {
                    var key = genericCollectionType.Namespace + "." + genericCollectionType.Name;
                    if (mapped.Any(m => m == key)) continue;

                    mapped.Add(key);
                }
                else if (arrayType is not null)
                {
                    var key = arrayType.Namespace + "." + arrayType.Name;
                    if (mapped.Any(m => m == key)) continue;

                    mapped.Add(key);
                }

                var relationship = (Layers.Relationships,
                                    Generator.GetRelationships(field, fieldTypeName, arrayElementType, genericCollectionType, genericCollectionTypeName),
                                    MemberTypes.None);

                var memberHolder = CreateMemberHolder(ObjectType.GetTypeInfo(), relationship);

                result.Segments.Add(memberHolder);
            }

            foreach (var property in ObjectType.GetRuntimeProperties())
            {
                if (property.DeclaringType.NormalizeName() != ObjectType.NormalizeName()) continue;
                if (property.PropertyType.Name.EndsWith(nameof(EventHandler))) continue;
                if (property.PropertyType.Name != NormalizeType(property.PropertyType.Name)) continue;

                var propertyTypeName =
                    property.PropertyType.IsGenericType
                        ? (property.PropertyType.GetGenericArguments().First()).NormalizeName()
                        : (property.PropertyType).NormalizeName();

                var genericCollectionType = property.PropertyType.Name.StartsWith("IEnumerable")
                                                ? property.PropertyType.GetGenericArguments().First()
                                                : typeof(object);

                var genericCollectionTypeName = genericCollectionType.NormalizeName();

                var arrayType = Generator.GetArrayType<TGenerator>(property.PropertyType);
                var arrayElementType = arrayType.NormalizeName();

                if (arrayElementType is null)
                {
                    var key = genericCollectionType.Namespace + "." + genericCollectionType.Name;
                    if (mapped.Any(m => m == key)) continue;

                    mapped.Add(key);
                }
                else if (arrayType is not null)
                {
                    var key = arrayType.Namespace + "." + arrayType.Name;
                    if (mapped.Any(m => m == key)) continue;

                    mapped.Add(key);
                }

                var relationship = (Layers.Relationships,
                                    Generator.GetRelationships(property, propertyTypeName, arrayElementType, genericCollectionType, genericCollectionTypeName),
                                    MemberTypes.None);

                var memberHolder = CreateMemberHolder(ObjectType.GetTypeInfo(), relationship);

                result.Segments.Add(memberHolder);
            }
        }

        private IEnumerable<(ConstructorInfo ctor, string segment)> BuildConstructors(
            Layers layers, bool showAttributes)
        {
            var bindingFlags = BindingFlags.Instance | layers switch
                                                       {
                                                           Layers.All => BindingFlags.Public | BindingFlags.NonPublic,
                                                           Layers.Members => BindingFlags.Public |
                                                                             BindingFlags.NonPublic,
                                                           Layers.NonPublic => BindingFlags.NonPublic,
                                                           Layers.Public => BindingFlags.Public,
                                                           _ => BindingFlags.Default
                                                       };

            var ctors = ObjectType.GetConstructors(bindingFlags);

            if (!ctors.Any()) yield break;

            foreach (var ctor in ctors)
            {
                if (ctor.DeclaringType != ObjectType) continue;

                yield return (ctor, Generator.GetMember(ctor, Generator.GetAttributes(ctor.GetCustomAttributesData(), showAttributes)));
            }
        }

        private IEnumerable<string> BuildMethod(MethodInfo? member, bool showAttributes)
        {
            if (member is null || member.Name.StartsWith("add_") || member.Name.StartsWith("remove_")) yield break;

            IEnumerable<MethodInfo> methods = ObjectType.GetRuntimeMethods().Where(e => e.Name == member.Name).ToList();

            if (!methods.Any()) yield break;

            foreach (MethodInfo method in methods)
            {
                if (method.DeclaringType != ObjectType) continue;

                var methodName = method.Name;

                MethodInfo? genericMethod = null;

                if (method.IsGenericMethod)
                    genericMethod = method.GetGenericMethodDefinition();
                else if (method.IsGenericMethodDefinition) genericMethod = method;

                if (genericMethod is not null)
                {
                    var genericTypes = string.Join(", ",
                                                   genericMethod.GetGenericArguments().Select(t => t.NormalizeName()));
                    genericTypes = $"<{genericTypes}>";

                    methodName += genericTypes;
                }

                yield return Generator.GetMember(method, methodName,
                                                  Generator.GetAttributes(method.GetCustomAttributesData(),
                                                                           showAttributes));
            }
        }

        private IEnumerable<string> BuildField(FieldInfo? field, bool showAttributes)
        {
            if (field is null) yield break;

            if (field.FieldType.Name.EndsWith(nameof(EventHandler))) yield break;

            if (field.DeclaringType != ObjectType) yield break;

            if (field.Name.EndsWith("_BackingField")) yield break;

            var isSpecialName = (field.Attributes & FieldAttributes.SpecialName) == FieldAttributes.SpecialName;
            if (isSpecialName) yield break;

            var attributes = Generator.GetAttributes(field.FieldType.GetCustomAttributesData(), showAttributes);

            yield return Generator.GetMember(field, attributes);
        }


        private IEnumerable<string> BuildProperty(PropertyInfo? member, bool showAttributes)
        {
            if (member is null) yield break;

            IEnumerable<PropertyInfo> properties = ObjectType.GetRuntimeProperties()
                                                             .Where(e => e.Name == member.Name).ToList();

            if (!properties.Any()) yield break;

            foreach (var property in properties)
            {
                if (property.DeclaringType != ObjectType) continue;

                var attributes = Generator.GetAttributes(property.PropertyType.GetCustomAttributesData(), showAttributes);

                yield return Generator.GetMember(property, attributes);
            }
        }

        private IEnumerable<string> BuildEvent(EventInfo? member, bool showAttributes)
        {
            if (member is null) yield break;

            var events = ObjectType.GetRuntimeEvents().Where(e => e.Name == member.Name).ToList();

            if (!events.Any()) yield break;

            foreach (var evt in events)
            {
                if (evt.DeclaringType != ObjectType) continue;

                yield return Generator.GetMember(evt, Generator.GetAttributes(evt.GetCustomAttributesData(), showAttributes));
            }
        }


    }
}
