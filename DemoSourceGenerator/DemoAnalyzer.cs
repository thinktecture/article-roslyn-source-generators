using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DemoSourceGenerator
{
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   public class DemoAnalyzer : DiagnosticAnalyzer
   {
      public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
         = ImmutableArray.Create(DemoDiagnosticsDescriptors.EnumerationMustBePartial);

      public override void Initialize(AnalysisContext context)
      {
         context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
         context.EnableConcurrentExecution();

         context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
      }

   private static void AnalyzeNamedType(SymbolAnalysisContext context)
   {
      if (!DemoSourceGenerator.IsEnumeration(context.Symbol))
         return;

      var type = (INamedTypeSymbol)context.Symbol;

      foreach (var declaringSyntaxReference in type.DeclaringSyntaxReferences)
      {
         if (declaringSyntaxReference.GetSyntax() is not ClassDeclarationSyntax classDeclaration ||
             DemoSyntaxReceiver.IsPartial(classDeclaration))
            continue;

         var error = Diagnostic.Create(DemoDiagnosticsDescriptors.EnumerationMustBePartial,
                                       classDeclaration.Identifier.GetLocation(),
                                       type.Name);
         context.ReportDiagnostic(error);
      }
   }
   }
}
