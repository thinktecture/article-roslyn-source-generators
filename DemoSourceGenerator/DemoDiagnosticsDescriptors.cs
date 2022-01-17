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
}