namespace DemoTests;

public class GeneratedCode
{
   public string FromDemoCodeGenerator { get; }
   public string FromTranslationsCodeGenerator { get; }
   public string FromNewtonsoftCodeGenerator { get; }

   public GeneratedCode(
      string fromDemoCodeGenerator,
      string fromTranslationsCodeGenerator,
      string fromNewtonsoftCodeGenerator)
   {
      FromDemoCodeGenerator = fromDemoCodeGenerator;
      FromTranslationsCodeGenerator = fromTranslationsCodeGenerator;
      FromNewtonsoftCodeGenerator = fromNewtonsoftCodeGenerator;
   }
}
