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
   public void Initialize(IncrementalGeneratorInitializationContext context)
   {
      var enumTypes = context.SyntaxProvider
                             .ForAttributeWithMetadataName("DemoLibrary.EnumGenerationAttribute",
                                                           CouldBeEnumerationAsync,
                                                           GetEnumInfo)
                             .Collect()
                             .SelectMany((enumInfos, _) => enumInfos.Distinct());

      var generators = context.GetMetadataReferencesProvider()
                              .SelectMany(static (reference, _) => TryGetCodeGenerator(reference, out var factory)
                                                                      ? ImmutableArray.Create(factory)
                                                                      : ImmutableArray<ICodeGenerator>.Empty)
                              .Collect();

      context.RegisterSourceOutput(enumTypes.Combine(generators), GenerateCode);

      InitializeTranslationsGenerator(context, enumTypes);
   }

   private static void InitializeTranslationsGenerator(
      IncrementalGeneratorInitializationContext context,
      IncrementalValuesProvider<DemoEnumInfo> enumTypes)
   {
      var enumNames = enumTypes.Select((t, _) => (t.Namespace, t.Name));

      var mergedTranslations = context.AdditionalTextsProvider
                                      .Where(text => text.Path.EndsWith("translations.json", StringComparison.OrdinalIgnoreCase))
                                      .Select((text, token) => text.GetText(token)?.ToString())
                                      .Select((json, _) => ParseTranslations(json))
                                      .Where(translations => !translations.IsEmpty)
                                      .Collect()
                                      .Select(MergeTranslations);

      var translationInfos = enumNames.Combine(mergedTranslations)
                                      .SelectMany((tuple, _) =>
                                                  {
                                                     var (ns, name) = tuple.Left;
                                                     var translationsByClassName = tuple.Right;

                                                     if (!translationsByClassName.TryGetValue(name, out var translations))
                                                        return ImmutableArray<EnumTranslationInfo>.Empty;

                                                     var translationInfo = new EnumTranslationInfo(ns, name, translations);
                                                     return ImmutableArray.Create(translationInfo);
                                                  });

      context.RegisterImplementationSourceOutput(translationInfos, GenerateCode);
   }

   private static ImmutableDictionary<string, ImmutableDictionary<string, string>> MergeTranslations(
      ImmutableArray<ImmutableDictionary<string, ImmutableDictionary<string, string>>> collectedTranslations,
      CancellationToken cancellationToken)
   {
      if (collectedTranslations.IsDefaultOrEmpty)
         return ImmutableDictionary<string, ImmutableDictionary<string, string>>.Empty;

      if (collectedTranslations.Length == 1)
         return collectedTranslations[0];

      var mergedTranslations = ImmutableDictionary<string, ImmutableDictionary<string, string>>.Empty;

      foreach (var translationsByClassName in collectedTranslations)
      {
         cancellationToken.ThrowIfCancellationRequested();

         foreach (var kvp in translationsByClassName)
         {
            try
            {
               var className = kvp.Key;
               var translationByLanguage = kvp.Value;

               if (mergedTranslations.TryGetValue(className, out var otherTranslations))
                  translationByLanguage = translationByLanguage.AddRange(otherTranslations);

               mergedTranslations = mergedTranslations.SetItem(className, translationByLanguage);
            }
            catch (Exception)
            {
               // Report the error
            }
         }
      }

      return mergedTranslations;
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
      (DemoEnumInfo, ImmutableArray<ICodeGenerator>) args)
   {
      var (enumInfo, generators) = args;

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

   private static ImmutableDictionary<string, ImmutableDictionary<string, string>> ParseTranslations(string? json)
   {
      if (String.IsNullOrWhiteSpace(json))
         return ImmutableDictionary<string, ImmutableDictionary<string, string>>.Empty;

      try
      {
         return JsonConvert.DeserializeObject<ImmutableDictionary<string, ImmutableDictionary<string, string>>>(json!)
                ?? ImmutableDictionary<string, ImmutableDictionary<string, string>>.Empty;
      }
      catch (Exception)
      {
         return ImmutableDictionary<string, ImmutableDictionary<string, string>>.Empty;
      }
   }

   private static void GenerateCode(
      SourceProductionContext context,
      EnumTranslationInfo translationInfo)
   {
      var ns = translationInfo.Namespace is null ? null : $"{translationInfo.Namespace}.";
      var code = EnumTranslationsGenerator.Instance.Generate(translationInfo);

      if (!String.IsNullOrWhiteSpace(code))
         context.AddSource($"{ns}{translationInfo.Name}.Translations.g.cs", code);
   }
}
