namespace DemoLibrary;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class TranslationAttribute : Attribute
{
   public string Language { get; }
   public string Translation { get; }

   public TranslationAttribute(string language, string translation)
   {
      Language = language;
      Translation = translation;
   }
}
