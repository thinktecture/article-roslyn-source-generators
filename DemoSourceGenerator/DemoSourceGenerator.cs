using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;

namespace DemoSourceGenerator;

[Generator]
public class DemoSourceGenerator : IIncrementalGenerator
{
   private static readonly IReadOnlyDictionary<string, string> _noTranslations = new Dictionary<string, string>();

   public void Initialize(IncrementalGeneratorInitializationContext context)
   {
      var enumTypes = context.SyntaxProvider
                             .ForAttributeWithMetadataName("DemoLibrary.EnumGenerationAttribute",
                                                           CouldBeEnumerationAsync,
                                                           GetEnumInfo)
                             .Collect()
                             .SelectMany((enumInfos, _) => enumInfos.Distinct());

      var translations = context.AdditionalTextsProvider
                                .Where(text => text.Path.EndsWith("translations.json", StringComparison.OrdinalIgnoreCase))
                                .Select((text, token) => text.GetText(token)?.ToString())
                                .Where(text => text is not null)!
                                .Collect<string>();

      var generators = context.GetMetadataReferencesProvider()
                              .SelectMany(static (reference, _) => TryGetCodeGenerator(reference, out var factory)
                                                                      ? ImmutableArray.Create(factory)
                                                                      : ImmutableArray<ICodeGenerator>.Empty)
                              .Collect();

      context.RegisterSourceOutput(enumTypes.Combine(translations).Combine(generators), GenerateCode);
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
      return syntaxNode is ClassDeclarationSyntax classDeclaration &&
             IsPartial(classDeclaration);
   }

   private static DemoEnumInfo GetEnumInfo(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
   {
      var type = (INamedTypeSymbol)context.TargetSymbol;
      var enumInfo = new DemoEnumInfo(type);

      return enumInfo;
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
      ((DemoEnumInfo, ImmutableArray<string>), ImmutableArray<ICodeGenerator>) args)
   {
      var ((enumInfo, translationsAsJson), generators) = args;

      if (generators.IsDefaultOrEmpty)
         return;

      var translationsByClassName = GetTranslationsByClassName(context, translationsAsJson);

      foreach (var generator in generators.Distinct())
      {
         if (translationsByClassName is null || !translationsByClassName.TryGetValue(enumInfo.Name, out var translations))
            translations = _noTranslations;

         var ns = enumInfo.Namespace is null ? null : $"{enumInfo.Namespace}.";
         var code = generator.Generate(enumInfo, translations);

         if (!String.IsNullOrWhiteSpace(code))
            context.AddSource($"{ns}{enumInfo.Name}{generator.FileHintSuffix}.g.cs", code);
      }
   }

   private static Dictionary<string, IReadOnlyDictionary<string, string>>? GetTranslationsByClassName(SourceProductionContext context, ImmutableArray<string> translationsAsJson)
   {
      if (translationsAsJson.Length <= 0)
         return null;

      if (translationsAsJson.Length > 1)
      {
         var error = Diagnostic.Create(DemoDiagnosticsDescriptors.MultipleTranslationsFound, null);
         context.ReportDiagnostic(error);
      }

      try
      {
         return JsonConvert.DeserializeObject<Dictionary<string, IReadOnlyDictionary<string, string>>>(translationsAsJson[0]);
      }
      catch (Exception ex)
      {
         var error = Diagnostic.Create(DemoDiagnosticsDescriptors.TranslationDeserializationError,
                                       null,
                                       ex.ToString());
         context.ReportDiagnostic(error);

         return null;
      }
   }
}
