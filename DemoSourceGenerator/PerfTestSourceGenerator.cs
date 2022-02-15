using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DemoSourceGenerator;

[Generator]
public class PerfTestSourceGenerator : IIncrementalGenerator
{
   private static int _counter;

   public void Initialize(IncrementalGeneratorInitializationContext context)
   {
      var classProvider = context.SyntaxProvider
                                 .CreateSyntaxProvider((node, _) =>
                                                       {
                                                          return node is ClassDeclarationSyntax;
                                                       },
                                                       (ctx, _) =>
                                                       {
                                                          return (ClassDeclarationSyntax)ctx.Node;
                                                       });

      context.RegisterSourceOutput(classProvider, Generate);
   }

   private static void Generate(SourceProductionContext ctx, ClassDeclarationSyntax cds)
   {
      Generate(ctx, cds.Identifier.Text);
   }

   private static void Generate(SourceProductionContext ctx, string name)
   {
      var ns = "DemoConsoleApplication";

      ctx.AddSource($"{ns}.{name}.perf.cs", $@"// <auto-generated />

// Counter: {Interlocked.Increment(ref _counter)}

namespace {ns}
{{
   partial class {name}
   {{
   }}
}}
");
   }
}