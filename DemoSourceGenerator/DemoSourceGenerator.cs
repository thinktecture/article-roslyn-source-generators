using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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

      var generators = context.GetMetadataReferencesProvider()
                              .SelectMany(static (reference, _) => TryGetCodeGenerator(reference, out var factory)
                                                                      ? ImmutableArray.Create(factory)
                                                                      : ImmutableArray<ICodeGenerator>.Empty)
                              .Collect();

      context.RegisterSourceOutput(enumTypes.Combine(generators), GenerateCode);
   }

   private static bool TryGetCodeGenerator(
      MetadataReference reference,
      [MaybeNullWhen(false)] out ICodeGenerator codeGenerator)
   {
      foreach (var module in reference.GetModules())
      {
         switch (module.Name)
         {
            case "DemoLibrary.dll":
               codeGenerator = DemoCodeGenerator.Instance;
               return true;

            case "Newtonsoft.Json.dll" when module.Version.Major >= 11:
               codeGenerator = NewtonsoftJsonSourceGenerator.Instance;
               return true;
         }
      }

      codeGenerator = null;
      return false;
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

   private static void GenerateCode(
      SourceProductionContext context,
      (DemoEnumInfo, ImmutableArray<ICodeGenerator>) tuple)
   {
      var (enumInfo, generators) = tuple;

      if (generators.IsDefaultOrEmpty)
         return;

      foreach (var generator in generators.Distinct())
      {
         var ns = enumInfo.Namespace is null ? null : $"{enumInfo.Namespace}.";
         var code = generator.Generate(enumInfo);

         if (!String.IsNullOrWhiteSpace(code))
            context.AddSource($"{ns}{enumInfo.Name}{generator.FileHintSuffix}.g.cs", code);
      }
   }
}
