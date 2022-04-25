using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DemoSourceGenerator.PerfTesting;

public class MyTupleComparer
   : IEqualityComparer<(ClassDeclarationSyntax Node, Compilation compilation)>
{
   public bool Equals(
      (ClassDeclarationSyntax Node, Compilation compilation) x,
      (ClassDeclarationSyntax Node, Compilation compilation) y)
   {
      return x.Node.Identifier.Text.Equals(y.Node.Identifier.Text);
   }

   public int GetHashCode((ClassDeclarationSyntax Node, Compilation compilation) obj)
   {
      return obj.Node.Identifier.Text.GetHashCode();
   }
}
