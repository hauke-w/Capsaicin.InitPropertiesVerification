using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace InitProperties.Generator
{
    /// <summary>
    /// Generates verification code for init properties annotated with "Required" attribute.
    /// The containing class must be annotated with "VerifiesInitProperties" attribute.
    /// </summary>
    [Generator]
    public class PartialClassWithInitPropertiesGenerator : ISourceGenerator
    {
        private const string VerifiesInitPropertiesAttributeFullName = "InitProperties.Reflection.VerifiesInitPropertiesAttribute";
        private const string RequiredAttributeFullName = "System.ComponentModel.DataAnnotations.RequiredAttribute";

        private const string DiagnosticIdPrefix = "IPG";
        private const string MessageCategory = "InitProperties.Generator";

        private const string MessageIdPropertyIsValueTypeButNotNullable = DiagnosticIdPrefix + "001";
        private const string MessagePropertyIsValueTypeButNotNullable = "The property '{0}' has value type but is not nullable. Therefore, it will not be verified.";
        private const string TitlePropertyIsValueTypeButNotNullable = "Property cannot be verified.";

        /// <inheritdoc/>
        public void Initialize(GeneratorInitializationContext context)
        {
            //Debugger.Launch(); // enable this line for debugging
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        /// <inheritdoc/>
        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is SyntaxReceiver syntaxReceiver)
            {
                foreach (var (typeSymbol, typeKind) in syntaxReceiver.Types)
                {
                    var source = GenerateVerifyInitProperties(typeSymbol, typeKind, context);
                    var hintName = typeSymbol.Name + "_VerifyInitProperties.cs";
                    context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
                }                
            }
        }

        private string GenerateVerifyInitProperties(INamedTypeSymbol typeSymbol, string typeKind, GeneratorExecutionContext context)
        {
            string namespaceName = typeSymbol.ContainingNamespace.ToDisplayString();
            bool isStruct = typeKind == "struct";
            bool isBase = isStruct || !HasVerifiesInitPropertiesAttributeRecursive(typeSymbol.BaseType);
            var (inheritanceModifier, visibility) = (isBase, typeKind) switch
            {
                (_, "struct") => (string.Empty, "private"),
                (true, _) => ("virtual ", "protected"),
                _ => ("override ", "protected")
            };
            List<string> localPropertyNames = GetInitProperties(typeSymbol, context);
            var memberNotNullAttribute = $"[MemberNotNull({string.Join(", ", localPropertyNames.Select(p => $"nameof({p})"))})]";

            var source = new StringBuilder($@"using System;
using System.Diagnostics.CodeAnalysis;

namespace {namespaceName}
{{
    partial {typeKind} {typeSymbol.Name}
    {{");

            if (!isStruct)
            {
                if (isBase)
                {
                    source.Append(@"
        public bool IsInitialized { get; private set; }");
                }

                source.Append($@"
        /// <summary>
        /// Verifies that all required init properties have a value that is not null
        /// unless this verification has already been done (i.e. <see cref=""IsInitialized""/> is true).
        /// </summary>
        /// <exception cref=""InvalidOperationException"">
        /// Thrown indirectly if <see cref=""IsInitialized""/> is <c>false</c> and any required
        /// init property has a value of null.
        /// </exception>
        {memberNotNullAttribute}
        protected {inheritanceModifier}void VerifyIsInitializedOnce()
        {{
            if (!IsInitialized)
            {{
                VerifyIsInitialized();
                IsInitialized = true;
            }}
        }}
");
            }

            source.Append($@"
        /// <summary>
        /// Verifies that all required init properties have a value that is not null.
        /// </summary>
        /// <exception cref=""InvalidOperationException"">
        /// Thrown if any required init property has a value of null.
        /// </exception>
        {memberNotNullAttribute}
        {visibility} {inheritanceModifier}void VerifyIsInitialized()
        {{
            var propertyName = GetNotInitializedPropertyName();
            if (propertyName is not null)
            {{
                throw new InvalidOperationException($""Property '{{propertyName}}' is not initialized."");
            }}
        }}

        {visibility} {inheritanceModifier}string? GetNotInitializedPropertyName()
        {{");

            foreach (var propertyName in localPropertyNames)
            {
                source.Append($@"
            if (nameof({propertyName}) is null)
            {{
                return nameof({propertyName});
            }}
");
            }

            source.Append($@"
            return {(isBase ? "null" : "base.GetNotInitializedPropertyName()")};
        }}
    }}
}}");
            return source.ToString();
        }

        private List<string> GetInitProperties(INamedTypeSymbol typeSymbol, GeneratorExecutionContext context)
        {
            var initProperties = new List<string>();
            foreach (var memberSymbol in typeSymbol.GetMembers())
            {
                if (memberSymbol is IPropertySymbol
                    {
                        IsStatic: false,
                        IsIndexer: false,
                        OverriddenProperty: null,
                        ExplicitInterfaceImplementations: { IsEmpty: true },
                        SetMethod: { IsInitOnly: true },
                        GetMethod: not null
                    } propertySymbol
                    && HasAttribute(memberSymbol, RequiredAttributeFullName))
                {
                    if (propertySymbol.Type.IsValueType && propertySymbol.Type.NullableAnnotation == NullableAnnotation.NotAnnotated)
                    {
                        ReportDiagnostic(context, propertySymbol, MessageIdPropertyIsValueTypeButNotNullable, TitlePropertyIsValueTypeButNotNullable, MessagePropertyIsValueTypeButNotNullable, DiagnosticSeverity.Warning);
                    }
                    else
                    {
                        initProperties.Add(propertySymbol.Name);
                    }
                }
            }
            return initProperties;
        }

        private static void ReportDiagnostic(GeneratorExecutionContext context, ISymbol forSymbol, string messageId, string title, string messageFormat, DiagnosticSeverity defaultSeverity, bool isEnabledByDefault = true)
        {
            var descriptor = new DiagnosticDescriptor(messageId, title, messageFormat, MessageCategory, defaultSeverity, isEnabledByDefault);
            var diagnostic = Diagnostic.Create(descriptor, forSymbol.Locations.FirstOrDefault(), forSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }

        private static bool HasVerifiesInitPropertiesAttributeRecursive(INamedTypeSymbol? typeSymbol)
        {
            return typeSymbol is not null
                && (HasVerifiesInitPropertiesAttribute(typeSymbol) || HasVerifiesInitPropertiesAttributeRecursive(typeSymbol.BaseType));
        }

        private static bool HasVerifiesInitPropertiesAttribute(INamedTypeSymbol typeSymbol)
            => HasAttribute(typeSymbol, VerifiesInitPropertiesAttributeFullName);

        private static bool HasAttribute(ISymbol symbol, string attributeFullName)
            => symbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == attributeFullName);

        private class SyntaxReceiver : ISyntaxContextReceiver
        {
            public List<(INamedTypeSymbol Symbol, string Kind)> Types { get; } = new();

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (context.Node is TypeDeclarationSyntax typeDeclarationSyntax
                    && context.SemanticModel.GetDeclaredSymbol(typeDeclarationSyntax) is INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct } typeSymbol
                    && HasVerifiesInitPropertiesAttribute(typeSymbol))
                {
                    var kind = typeSymbol.TypeKind switch
                    {
                        TypeKind.Struct => "struct",
                        _ when typeDeclarationSyntax is RecordDeclarationSyntax => "record",
                        _ => "class"
                    };
                    Types.Add((typeSymbol, kind));
                }
            }
        }
    }
}
