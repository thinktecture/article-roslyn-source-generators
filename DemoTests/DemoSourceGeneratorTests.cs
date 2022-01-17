using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DemoTests;

public class DemoSourceGeneratorTests
{
   [Fact]
   public void Should_generate_Items_property_with_2_items()
   {
      var input = @"
using DemoLibrary;

namespace DemoTests
{
   [EnumGeneration]
   public partial class ProductCategory
   {
      public static readonly ProductCategory Fruits = new(""Fruits"");
      public static readonly ProductCategory Dairy = new(""Dairy"");

      public string Name { get; }

      private ProductCategory(string name)
      {
         Name = name;
      }
   }
}
";
      GetGeneratedOutput(input)
         .Should().Be(@"// <auto-generated />

using System.Collections.Generic;

namespace DemoTests
{
   partial class ProductCategory
   {
      private static IReadOnlyList<ProductCategory> _items;
      public static IReadOnlyList<ProductCategory> Items => _items ??= GetItems();

      private static IReadOnlyList<ProductCategory> GetItems()
      {
         return new[] { Fruits, Dairy };
      }
   }
}
");
   }

   private static string? GetGeneratedOutput(string sourceCode)
   {
      var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
      var references = AppDomain.CurrentDomain.GetAssemblies()
                                .Where(assembly => !assembly.IsDynamic)
                                .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
                                .Cast<MetadataReference>();

      var compilation = CSharpCompilation.Create("SourceGeneratorTests",
                                                 new[] { syntaxTree },
                                                 references,
                                                 new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

      var generator = new DemoSourceGenerator.DemoSourceGenerator();
      CSharpGeneratorDriver.Create(generator)
                           .RunGeneratorsAndUpdateCompilation(compilation,
                                                              out var outputCompilation,
                                                              out var diagnostics);

      // optional
      diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
                 .Should().BeEmpty();

      return outputCompilation.SyntaxTrees.Skip(1).LastOrDefault()?.ToString();
   }
}
