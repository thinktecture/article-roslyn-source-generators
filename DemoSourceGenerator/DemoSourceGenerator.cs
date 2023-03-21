using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using DemoSourceGenerator.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Newtonsoft.Json;

namespace DemoSourceGenerator;

[Generator]
public class DemoSourceGenerator : IIncrementalGenerator
{
   private ILogger _logger = NullLogger.Instance;

   public void Initialize(IncrementalGeneratorInitializationContext context)
   {
      var options = GetGeneratorOptions(context);

      SetupLogger(context, options);

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

      context.RegisterSourceOutput(enumTypes.Combine(generators).Combine(options), GenerateCode);

      InitializeTranslationsGenerator(context, enumTypes);
   }

   private static IncrementalValueProvider<GeneratorOptions> GetGeneratorOptions(IncrementalGeneratorInitializationContext context)
   {
      return context.AnalyzerConfigOptionsProvider.Select((options, _) =>
                                                          {
                                                             var counterEnabled = options.GlobalOptions
                                                                                         .TryGetValue("build_property.DemoSourceGenerator_Counter", out var counterEnabledValue)
                                                                                  && IsFeatureEnabled(counterEnabledValue);

                                                             var loggingOptions = GetLoggingOptions(options);

                                                             return new GeneratorOptions(counterEnabled, loggingOptions);
                                                          });
   }

   private static LoggingOptions? GetLoggingOptions(AnalyzerConfigOptionsProvider options)
   {
      if (!options.GlobalOptions.TryGetValue("build_property.DemoSourceGenerator_LogFilePath", out var logFilePath))
         return null;

      if (String.IsNullOrWhiteSpace(logFilePath))
         return null;

      logFilePath = logFilePath.Trim();

      if (!options.GlobalOptions.TryGetValue("build_property.DemoSourceGenerator_LogLevel", out var logLevelValue)
          || !Enum.TryParse(logLevelValue, true, out LogLevel logLevel))
      {
         logLevel = LogLevel.Information;
      }

      return new LoggingOptions(logFilePath, logLevel);
   }

   private static bool IsFeatureEnabled(string counterEnabledValue)
   {
      return StringComparer.OrdinalIgnoreCase.Equals("enable", counterEnabledValue)
             || StringComparer.OrdinalIgnoreCase.Equals("enabled", counterEnabledValue)
             || StringComparer.OrdinalIgnoreCase.Equals("true", counterEnabledValue);
   }

   private void SetupLogger(
      IncrementalGeneratorInitializationContext context,
      IncrementalValueProvider<GeneratorOptions> optionsProvider)
   {
      var logging = optionsProvider
                    .Select((options, _) => options.Logging)
                    .Select((options, _) =>
                            {
                               _logger = options is null
                                            ? NullLogger.Instance
                                            : new Logger(options.Value.Level, options.Value.FilePath);

                               return 0;
                            })
                    .SelectMany((_, _) => ImmutableArray<int>.Empty); // don't emit anything

      context.RegisterSourceOutput(logging, static (_, _) =>
                                            {
                                               // This delegate will never be called
                                            });
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

   private DemoEnumInfo GetEnumInfo(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
   {
      var type = (INamedTypeSymbol)context.TargetSymbol;
      var enumInfo = new DemoEnumInfo(type);

      if (_logger.IsEnabled(LogLevel.Debug))
         _logger.Log(LogLevel.Debug, $"Smart Enum found: {enumInfo.Namespace}.{enumInfo.Name}");

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
      ((DemoEnumInfo, ImmutableArray<ICodeGenerator>), GeneratorOptions) args)
   {
      var ((enumInfo, generators), options) = args;

      if (generators.IsDefaultOrEmpty)
         return;

      foreach (var generator in generators.Distinct())
      {
         var ns = enumInfo.Namespace is null ? null : $"{enumInfo.Namespace}.";
         var code = generator.Generate(enumInfo, options);

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
