using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Capsaicin.InitPropertiesVerification.Generator
{
    partial class PartialClassWithInitPropertiesGenerator
    {
        private class SyntaxReceiver : ISyntaxContextReceiver
        {
            public List<(INamedTypeSymbol Symbol, string Kind)> Types { get; } = new();

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                var td = context.Node as TypeDeclarationSyntax;
                if (context.Node is TypeDeclarationSyntax typeDeclarationSyntax
                    && context.SemanticModel.GetDeclaredSymbol(typeDeclarationSyntax) is INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct } typeSymbol
                    && HasVerifiesInitPropertiesAttribute(typeSymbol))
                {
                    
                    var kind = typeSymbol switch
                    {
                        { TypeKind: TypeKind.Struct, IsRecord:true } => "record struct",
                        { TypeKind: TypeKind.Struct } => "struct",
                        { IsRecord: true } => "record class",
                        _ => "class"
                    };
                    Types.Add((typeSymbol, kind));
                }
            }
        }
    }
}
