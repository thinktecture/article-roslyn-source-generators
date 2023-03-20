using DemoLibrary;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace DemoTests.Verifiers;

public static class AnalyzerVerifier<TAnalyzer>
   where TAnalyzer : DiagnosticAnalyzer, new()
{
   public static DiagnosticResult Diagnostic(string diagnosticId)
   {
      return CSharpAnalyzerVerifier<TAnalyzer, XUnitVerifier>.Diagnostic(diagnosticId);
   }

   public static async Task VerifyAnalyzerAsync(
      string source,
      params DiagnosticResult[] expected)
   {
      var test = new AnalyzerTest(source, expected);
      await test.RunAsync(CancellationToken.None);
   }

   private class AnalyzerTest : CSharpAnalyzerTest<TAnalyzer, XUnitVerifier>
   {
      public AnalyzerTest(
         string source,
         params DiagnosticResult[] expected)
      {
         TestCode = source;
         ExpectedDiagnostics.AddRange(expected);
#if NET7_0
         ReferenceAssemblies = new ReferenceAssemblies("net7.0", new PackageIdentity("Microsoft.NETCore.App.Ref", "7.0.0"), Path.Combine("ref", "7.0.0"));
#elif NET6_0
         ReferenceAssemblies = ReferenceAssemblies.Net.Net60;
#else
         ReferenceAssemblies = ReferenceAssemblies.Net.Net50;
#endif

         TestState.AdditionalReferences.Add(typeof(EnumGenerationAttribute).Assembly);
      }
   }
}
