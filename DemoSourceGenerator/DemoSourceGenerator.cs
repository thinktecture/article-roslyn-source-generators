using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DemoSourceGenerator;

[Generator]
public class DemoSourceGenerator : IIncrementalGenerator
{
   public void Initialize(IncrementalGeneratorInitializationContext context)
   {
      var enumTypes = context.SyntaxProvider
                             .CreateSyntaxProvider(CouldBeEnumerationAsync, GetEnumInfoOrNull)
                             .Where(type => type is not null)!
                             .Collect<DemoEnumInfo>()
                             .SelectMany((enumInfos, _) => enumInfos.Distinct());

      context.RegisterSourceOutput(enumTypes, GenerateCode!);
   }

   private static bool CouldBeEnumerationAsync(SyntaxNode syntaxNode, CancellationToken cancellationToken)
   {
      if (syntaxNode is not AttributeSyntax attribute)
         return false;

      var name = ExtractName(attribute.Name);

      if (name is not ("EnumGeneration" or "EnumGenerationAttribute"))
         return false;

      // "attribute.Parent" is "AttributeListSyntax"
      // "attribute.Parent.Parent" is a C# fragment the attributes are applied to
      return attribute.Parent?.Parent is ClassDeclarationSyntax classDeclaration &&
             IsPartial(classDeclaration);
   }

   private static string? ExtractName(NameSyntax? name)
   {
      return name switch
      {
         SimpleNameSyntax ins => ins.Identifier.Text,
         QualifiedNameSyntax qns => qns.Right.Identifier.Text,
         _ => null
      };
   }

   private static DemoEnumInfo? GetEnumInfoOrNull(GeneratorSyntaxContext context, CancellationToken cancellationToken)
   {
      var classDeclaration = (ClassDeclarationSyntax)context.Node.Parent!.Parent!;

      var type = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, classDeclaration) as ITypeSymbol;

      return type is null || !IsEnumeration(type) ? null : new DemoEnumInfo(type);
   }

   public static bool IsPartial(ClassDeclarationSyntax classDeclaration)
   {
      return classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
   }

   public static bool IsEnumeration(ISymbol type)
   {
      return type.GetAttributes()
                 .Any(a => a.AttributeClass?.Name == "EnumGenerationAttribute" &&
                           a.AttributeClass.ContainingNamespace is
                           {
                              Name: "DemoLibrary",
                              ContainingNamespace.IsGlobalNamespace: true
                           });
   }

   private static void GenerateCode(SourceProductionContext context, DemoEnumInfo enumInfo)
   {
      var code = DemoCodeGenerator.Instance.Generate(enumInfo);
      var ns = enumInfo.Namespace is null ? null : $"{enumInfo.Namespace}.";

      context.AddSource($"{ns}{enumInfo.Name}.g.cs", code);
   }
}
