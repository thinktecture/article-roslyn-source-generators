using Microsoft.CodeAnalysis;

namespace DemoSourceGenerator;

public static class DemoDiagnosticsDescriptors
{
   public static readonly DiagnosticDescriptor EnumerationMustBePartial
      = new("DEMO001",                               // id
            "Enumeration must be partial",           // title
            "The enumeration '{0}' must be partial", // message
            "DemoAnalyzer",                          // category
            DiagnosticSeverity.Error,
            true);

   public static readonly DiagnosticDescriptor MultipleTranslationsFound
      = new("DEMO002",
            "Multiple translations found",
            "Multiple translations found",
            "DemoSourceGenerator",
            DiagnosticSeverity.Error,
            true);

   public static readonly DiagnosticDescriptor TranslationDeserializationError
      = new("DEMO003",
            "Translations could not be deserialized",
            "Translations could not be deserialized: {0}",
            "DemoSourceGenerator",
            DiagnosticSeverity.Error,
            true);
}
