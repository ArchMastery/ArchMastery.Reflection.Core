#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ArchMastery.Reflector.Core;
using ArchMastery.Reflector.Core.Base;
using MemberTypes = ArchMastery.Reflector.Core.Base.MemberTypes;

namespace ArchMastery.Structurizer.Reflector
{
    public class StructurizerGenerator : GeneratorBase<StructurizerClip>
    {
        /// <inheritdoc />
        public override string StartType(TypeInfo type, bool showAttributes, string attributes)
        {
            var objectType = GetObjectType();

            var staticObject = type.IsAbstract && type.IsSealed
                                   ? "<< static >> "
                                   : string.Empty;


            return @$"
{objectType} ""{DisplayName}"" as {TypeFullName.AsSlug()} 
{objectType} {TypeFullName.AsSlug()} {staticObject}{{" +
                   (showAttributes ? $"\n\t--- attributes ---\n{attributes}" : string.Empty);
        }

        /// <inheritdoc />
        public override string EndType(TypeInfo type) => "}\n";

        public override string GetExtends(string innerBaseName)
        {
            var skip = Exclusions.Any(innerBaseName.StartsWith);
            return !skip ? $"{Slug} -u-|> {innerBaseName.AsSlug()} : extends" : string.Empty;
        }

        /// <inheritdoc />
        public override string GetImplements(TypeInfo inheritsTypeInfo)
            => $"{Slug} --() {inheritsTypeInfo.NormalizeName()!.AsSlug()} : implements";

        public override string GetRelationships(FieldInfo field, string? fieldTypeName, string? arrayElementTypeName,
                                                Type? genericCollectionType, string? genericCollectionTypeName)
        {
            return field.FieldType.IsArray
                       ? $"{Slug} o- {(arrayElementTypeName ?? "").AsSlug()} : {field.Name} << aggregation >> "
                       : genericCollectionType != typeof(object)
                           ? $"{Slug} o- {genericCollectionTypeName!.AsSlug()} : {field.Name} << aggregation >>"
                           : $"{Slug} -> {fieldTypeName!.AsSlug()} : {field.Name} << use >>";
        }

        public override string GetRelationships(PropertyInfo property, string? propertyTypeName,
                                                string? arrayElementTypeName, Type? genericCollectionType,
                                                string? genericCollectionTypeName)
        {
            return property.PropertyType.IsArray
                       ? $"{Slug} o- {arrayElementTypeName?.AsSlug()} : {property.Name} << aggregation >>"
                       : genericCollectionType != typeof(object)
                           ? $"{Slug} o- {genericCollectionTypeName!.AsSlug()} : {property.Name} << aggregation >>"
                           : $"{Slug} -> {propertyTypeName!.AsSlug()} : {property.Name} << use >>";
        }

        /// <inheritdoc />
        public override string GetMember(FieldInfo field, string attributes)
        {
            var isPublic = field.IsPublic;
            var isPrivate = field.IsPrivate;
            var isFamily = field.IsFamily;

            return $"{attributes}" +
                $"\t{GetAccessibility(field.IsStatic, false, isPublic, isPrivate, isFamily, field.IsAssembly)}" +
                $"{field.Name.NormalizeNameString()}" +
                $": {(GetArrayType<StructurizerGenerator>(field.FieldType)).NormalizeName() ?? field.FieldType.Name}\n";
        }

        /// <inheritdoc />
        public override string GetMember(PropertyInfo property, string attributes)
        {
            var parList = property.GetIndexParameters();
            var parms = MakeParList(parList);

            var indexerParameters = string.IsNullOrWhiteSpace(parms)
                                        ? string.Empty
                                        : $"[{parms}]";

            var accessors = property.GetAccessors(true);
            var acc = accessors
                     .Select(accessor =>
                                 $"{GetAccessibility(accessor.IsStatic, accessor.IsAbstract, accessor.IsPublic, accessor.IsPrivate, accessor.IsFamily, false)}{accessor.Name.NormalizeNameString()}{indexerParameters}")
                     .ToList();

            var isPublic = (property.GetMethod?.IsPublic ?? false) || (property.SetMethod?.IsPublic ?? false);
            var isPrivate = (property.GetMethod?.IsPrivate ?? true) && !(property.SetMethod?.IsPublic ?? false);
            var isFamily = (property.GetMethod?.IsFamily ?? false) && !(property.SetMethod?.IsPublic ?? false);

            return $"{attributes}" +
                $"\t{GetAccessibility(property.GetMethod?.IsStatic ?? false, property.GetMethod?.IsAbstract ?? false, isPublic, isPrivate, isFamily, property.GetMethod?.IsAssembly ?? false)}" +
                $"{property.Name.NormalizeNameString()} " +
                $"({string.Join(" ", acc)}) " +
                $": {(GetArrayType<StructurizerGenerator>(property.PropertyType) ?? property.PropertyType).NormalizeName()} << property >>\n";
        }

        /// <inheritdoc />
        public override string GetMember(MethodInfo method, string methodName, string attributes)
        {
            var parList = method.GetParameters();
            var parms = MakeParList(parList);

            return $"{attributes}" +
                $"\t{GetAccessibility(method.IsStatic, method.IsAbstract, method.IsPublic, method.IsPrivate, method.IsFamily, method.IsAssembly)}" +
                $"{methodName.NormalizeNameString()}({parms})" +
                $": {method.ReturnType.NormalizeName()}\n";
        }

        /// <inheritdoc />
        public override string GetMember(ConstructorInfo ctor, string attributes)
        {
            var parList = ctor.GetParameters();

            var parms = MakeParList(parList);

            return $"{attributes}" +
                   $"\t{GetAccessibility(ctor.IsStatic, ctor.IsAbstract, ctor.IsPublic, ctor.IsPrivate, ctor.IsFamily, ctor.IsAssembly)}" +
                   $"ctor({parms})\n";
        }


        /// <inheritdoc />
        public override string GetMember(EventInfo evt, string attributes)
        {
            var delegateType = evt.EventHandlerType!;
            var method = delegateType.GetMethod("Invoke")!;

            var parList = method.GetParameters();
            var parms = MakeParList(parList);

            return $"{attributes}" +
                $"\t{GetAccessibility(method.IsStatic, method.IsAbstract, method.IsPublic, method.IsPrivate, method.IsFamily, method.IsAssembly)}" +
                $"{evt.Name.NormalizeNameString()}({parms}) : {method.ReturnType.NormalizeName()} << event >>\n";
        }

        public override string GetAttributes(ICollection<CustomAttributeData> attributes, bool showAttributes)
        {
            return showAttributes && attributes.Count > 0
                       ? "\t[" + string.Join(", ",
                                             attributes
                                                .Where(a => a.AttributeType != typeof(TypeForwardedFromAttribute))
                                                .Select(a =>
                                                            a.AttributeType.Name.Replace("Attribute", string.Empty) +
                                                            (a.ConstructorArguments.Count > 0
                                                                 ? "(" +
                                                                   string.Join(", ",
                                                                               a.ConstructorArguments
                                                                                  .Select(b => b.ToString())) +
                                                                   ")"
                                                                 : string.Empty))) + "]\n"
                       : string.Empty;
        }


        public override string CreatDivider(char dividerChar, MemberTypes memberType)
            => $"\t{new string(dividerChar, 3)} {GetMemberTypeString(memberType)} {new string(dividerChar, 3)}";

        private static string GetMemberTypeString(MemberTypes memberType)
            => memberType switch
               {
                   ArchMastery.Reflector.Core.Base.MemberTypes.Ctor => "constructors",
                   ArchMastery.Reflector.Core.Base.MemberTypes.Event => "events",
                   ArchMastery.Reflector.Core.Base.MemberTypes.Field => "fields",
                   ArchMastery.Reflector.Core.Base.MemberTypes.Method => "methods",
                   ArchMastery.Reflector.Core.Base.MemberTypes.Attribute => "attributes",
                   ArchMastery.Reflector.Core.Base.MemberTypes.Property => "properties",
                   _ => ""
               };

        public override string MakeParList(IEnumerable<ParameterInfo> parameters)
        {
            var p = parameters
                   .Select(par => ($"{par.Name!.NormalizeNameString()}" +
                                   $": {par.ParameterType.NormalizeName()} " +
                                   $"{(par.HasDefaultValue ? $" = {par.DefaultValue}" : "")}").Trim())
                   .ToArray();

            return string.Join(", ", p).Trim();
        }


        public override string GetAccessibility(bool isStatic, bool isAbstract, bool isPublic,
                                                bool isPrivate, bool isFamily, bool isAssembly)
        {
            var accessibility = GetAccessibility(isPublic, isPrivate, isFamily, isAssembly);
            var modifiers = isStatic ? "{static} " : "";
            modifiers += isAbstract ? "{abstract} " : "";
            return $"{modifiers}{accessibility}";
        }

        public override string GetAccessibility(bool methodIsPublic, bool methodIsPrivate, bool methodIsFamily,
                                                bool isAssembly)
        {
            return methodIsPublic
                       ? "+"
                       : methodIsFamily
                           ? "#"
                           : methodIsPrivate
                               ? "-"
                               : isAssembly
                                   ? "~"
                                   : "";
        }
    }
}
