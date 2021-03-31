using Capsaicin.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace InitProperties.Generator
{
    /// <summary>
    /// Generates verification code for init properties annotated with "Required" attribute.
    /// The containing class must be annotated with "VerifiesInitProperties" attribute.
    /// </summary>
    [Generator]
    public partial class PartialClassWithInitPropertiesGenerator : ISourceGenerator
    {
        private const string VerifiesInitPropertiesAttributeFullName = "InitProperties.Reflection.VerifiesInitPropertiesAttribute";
        private const string RequiredAttributeFullName = "System.ComponentModel.DataAnnotations.RequiredAttribute";

        private const string DiagnosticIdPrefix = "IPG";
        private const string MessageCategory = "InitProperties.Generator";

        private static DiagnosticDescriptor WarningIdPropertyIsValueTypeButNotNullable => new DiagnosticDescriptor(
            DiagnosticIdPrefix + "001",
            "Property cannot be verified",
            "The property '{0}' has value type but is not nullable. Therefore, it will not be verified.",
            MessageCategory,
            DiagnosticSeverity.Warning,
            true);

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
                var executeContext = new ExecuteContext(context, syntaxReceiver);
                executeContext.Generate();                
            }
        }

        private static bool HasVerifiesInitPropertiesAttributeRecursive(INamedTypeSymbol? typeSymbol)
        {
            return typeSymbol is not null
                && (HasVerifiesInitPropertiesAttribute(typeSymbol) || HasVerifiesInitPropertiesAttributeRecursive(typeSymbol.BaseType));
        }

        private static bool HasVerifiesInitPropertiesAttribute(INamedTypeSymbol typeSymbol)
            => typeSymbol.HasAttribute(VerifiesInitPropertiesAttributeFullName);
    }
}
