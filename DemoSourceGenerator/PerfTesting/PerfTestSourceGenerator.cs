using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DemoSourceGenerator.PerfTesting;

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
                                                          var cds = (ClassDeclarationSyntax)ctx.Node;

                                                          // use the semantic model if necessary
                                                          // var model = ctx.SemanticModel.GetDeclaredSymbol(cd);

                                                          return new MyCustomObject(cds.Identifier.Text);
                                                       })
                                 .Collect()
                                 .SelectMany((myObjects, _) => myObjects.Distinct());

      context.RegisterSourceOutput(classProvider, Generate);
   }

   private static void Generate(SourceProductionContext ctx, ImmutableArray<MyCustomObject> myCustomObjects)
   {
      foreach (var obj in myCustomObjects.Distinct())
      {
         ctx.CancellationToken.ThrowIfCancellationRequested();

         Generate(ctx, obj.Name);
      }
   }

   private static void Generate(SourceProductionContext ctx, MyCustomObject myCustomObject)
   {
      Generate(ctx, myCustomObject.Name);
   }

   private static void Generate(SourceProductionContext ctx, ITypeSymbol symbol)
   {
      Generate(ctx, symbol.Name);
   }

   private static void Generate(
      SourceProductionContext ctx,
      (ClassDeclarationSyntax, Compilation) tuple)
   {
      var (node, compilation) = tuple;
      var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);

      Generate(ctx, node);
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
