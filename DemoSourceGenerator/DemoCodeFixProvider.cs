using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DemoSourceGenerator
{
   [ExportCodeFixProvider(LanguageNames.CSharp)]
   public class DemoCodeFixProvider : CodeFixProvider
   {
      public override ImmutableArray<string> FixableDiagnosticIds { get; }
         = ImmutableArray.Create(DemoDiagnosticsDescriptors.EnumerationMustBePartial.Id);

      public override FixAllProvider GetFixAllProvider()
      {
         return WellKnownFixAllProviders.BatchFixer;
      }

      public override Task RegisterCodeFixesAsync(CodeFixContext context)
      {
         foreach (var diagnostic in context.Diagnostics)
         {
            if (diagnostic.Id != DemoDiagnosticsDescriptors.EnumerationMustBePartial.Id)
               continue;

            var title = DemoDiagnosticsDescriptors.EnumerationMustBePartial.Title.ToString();
            var action = CodeAction.Create(title,
                                           token => AddPartialKeywordAsync(context, diagnostic, token),
                                           title);
            context.RegisterCodeFix(action, diagnostic);
         }

         return Task.CompletedTask;
      }

      private static async Task<Document> AddPartialKeywordAsync(
         CodeFixContext context,
         Diagnostic makePartial,
         CancellationToken cancellationToken)
      {
         var root = await context.Document.GetSyntaxRootAsync(cancellationToken);

         if (root is null)
            return context.Document;

         var classDeclaration = FindClassDeclaration(makePartial, root);

         var partial = SyntaxFactory.Token(SyntaxKind.PartialKeyword);
         var newDeclaration = classDeclaration.AddModifiers(partial);
         var newRoot = root.ReplaceNode(classDeclaration, newDeclaration);
         var newDoc = context.Document.WithSyntaxRoot(newRoot);

         return newDoc;
      }

      private static ClassDeclarationSyntax FindClassDeclaration(
         Diagnostic makePartial,
         SyntaxNode root)
      {
         var diagnosticSpan = makePartial.Location.SourceSpan;

         return root.FindToken(diagnosticSpan.Start)
                    .Parent?.AncestorsAndSelf()
                    .OfType<ClassDeclarationSyntax>()
                    .First();
      }
   }
}
