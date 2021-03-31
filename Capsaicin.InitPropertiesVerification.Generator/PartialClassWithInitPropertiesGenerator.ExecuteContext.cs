using Capsaicin.CodeAnalysis.Extensions;
using Capsaicin.CodeAnalysis.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Capsaicin.InitPropertiesVerification.Generator
{
    partial class PartialClassWithInitPropertiesGenerator
    {
        private class ExecuteContext : ExecuteContextBase
        {
            public ExecuteContext(GeneratorExecutionContext generatorExecutionContext, SyntaxReceiver syntaxReceiver)
                : base(generatorExecutionContext)
            {
                SyntaxReceiver = syntaxReceiver;
            }

            private readonly SyntaxReceiver SyntaxReceiver;

            internal void Generate()
            {
                foreach (var (typeSymbol, typeKind) in SyntaxReceiver.Types)
                {
                    var source = GenerateVerifyInitProperties(typeSymbol, typeKind, GeneratorExecutionContext);
                    var hintName = typeSymbol.Name + "_VerifyInitProperties.cs";
                    GeneratorExecutionContext.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
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
        public bool IsInitialized { get; protected set; }");
                    }

                    source.Append($@"
        #nullable disable
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
        #nullable enable
");
                }

                source.Append($@"
        #nullable disable
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
        #nullable enable

        {visibility} {inheritanceModifier}string? GetNotInitializedPropertyName()
        {{");

                foreach (var propertyName in localPropertyNames)
                {
                    source.Append($@"
            if ({propertyName} is null)
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
                        && memberSymbol.HasAttribute(RequiredAttributeFullName))
                    {
                        if (propertySymbol.Type.IsValueType && propertySymbol.Type.NullableAnnotation == NullableAnnotation.NotAnnotated)
                        {
                            ReportDiagnostic(WarningIdPropertyIsValueTypeButNotNullable, propertySymbol);
                        }
                        else
                        {
                            initProperties.Add(propertySymbol.Name);
                        }
                    }
                }
                return initProperties;
            }
        }
    }
}
