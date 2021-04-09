using FluentAssertions;
using Xunit;

namespace DemoTests
{
   public class ItemsPropertyTests
   {
      [Fact]
      public void Should_return_all_known_items()
      {
         ProductCategory.Items.Should().HaveCount(2)
                        .And.BeEquivalentTo(ProductCategory.Fruits,
                                            ProductCategory.Dairy);
      }
   }
}
