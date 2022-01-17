using DemoLibrary;

namespace DemoTests;

[EnumGeneration]
public partial class ProductCategory
{
   public static readonly ProductCategory Fruits = new("Fruits");
   public static readonly ProductCategory Dairy = new("Dairy");

   public string Name { get; }

   private ProductCategory(string name)
   {
      Name = name;
   }
}
