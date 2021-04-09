using System.Collections.Generic;
using System.Threading.Tasks;
using DemoSourceGenerator;
using Xunit;
using Verifier = DemoTests.AnalyzerAndCodeFixVerifier<
   DemoSourceGenerator.DemoAnalyzer,
   DemoSourceGenerator.DemoCodeFixProvider>;

namespace DemoTests
{
   public class DemoAnalyzerAndCodeFixTests
   {
      [Fact]
      public async Task Should_trigger_on_non_partial_class()
      {
         var input = @"
using DemoLibrary;

namespace DemoTests
{
   [EnumGeneration]
   public class {|#0:ProductCategory|}
   {
   }
}";

         var expectedOutput = @"
using DemoLibrary;

namespace DemoTests
{
   [EnumGeneration]
   public partial class ProductCategory
   {
   }
}";

         var expectedError = Verifier.Diagnostic(DemoDiagnosticsDescriptors.EnumerationMustBePartial.Id)
                                     .WithLocation(0)
                                     .WithArguments("ProductCategory");
         await Verifier.VerifyCodeFixAsync(input, expectedOutput, expectedError);
      }
   }
}
