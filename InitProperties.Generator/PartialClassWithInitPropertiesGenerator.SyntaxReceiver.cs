using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace InitProperties.Generator
{
    partial class PartialClassWithInitPropertiesGenerator
    {
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
                        _ when typeSymbol.IsRecord => "record",
                        _ => "class"
                    };
                    Types.Add((typeSymbol, kind));
                }
            }
        }
    }
}
