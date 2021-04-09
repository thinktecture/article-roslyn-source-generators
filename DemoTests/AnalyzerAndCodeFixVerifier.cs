using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DemoLibrary;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace DemoTests
{
   public static class AnalyzerAndCodeFixVerifier<TAnalyzer, TCodeFix>
      where TAnalyzer : DiagnosticAnalyzer, new()
      where TCodeFix : CodeFixProvider, new()
   {
      public static DiagnosticResult Diagnostic(string diagnosticId)
      {
         return CSharpCodeFixVerifier<TAnalyzer, TCodeFix, XUnitVerifier>.Diagnostic(diagnosticId);
      }

      public static async Task VerifyCodeFixAsync(
         string source,
         string fixedSource,
         params DiagnosticResult[] expected)
      {
         var test = new CodeFixTest(source, fixedSource, expected);
         await test.RunAsync(CancellationToken.None);
      }

      private class CodeFixTest : CSharpCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
      {
         public CodeFixTest(
            string source,
            string fixedSource,
            params DiagnosticResult[] expected)
         {
            TestCode = source;
            FixedCode = fixedSource;
            ExpectedDiagnostics.AddRange(expected);
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50;

            TestState.AdditionalReferences.Add(typeof(EnumGenerationAttribute).Assembly);
         }
      }
   }
}
