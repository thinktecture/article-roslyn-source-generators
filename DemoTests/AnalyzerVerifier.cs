using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DemoLibrary;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace DemoTests
{
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
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50;

            TestState.AdditionalReferences.Add(typeof(EnumGenerationAttribute).Assembly);
         }
      }
   }
}
