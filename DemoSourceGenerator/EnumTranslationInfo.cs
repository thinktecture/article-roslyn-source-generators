using System.Collections.Immutable;

namespace DemoSourceGenerator;

public readonly struct EnumTranslationInfo : IEquatable<EnumTranslationInfo>
{
   public string? Namespace { get; }
   public string Name { get; }
   public ImmutableDictionary<string, string> Translations { get; }

   public EnumTranslationInfo(
      string? ns,
      string name,
      ImmutableDictionary<string, string> translations)
   {
      Namespace = ns;
      Name = name;
      Translations = translations;
   }

   public override bool Equals(object? obj)
   {
      return obj is EnumTranslationInfo other && Equals(other);
   }

   public bool Equals(EnumTranslationInfo other)
   {
      return Namespace == other.Namespace
             && Name == other.Name
             && Equal(Translations, other.Translations);
   }

   private static bool Equal(
      ImmutableDictionary<string, string> translations,
      ImmutableDictionary<string, string> otherTranslations)
   {
      if (translations.IsEmpty)
         return otherTranslations.IsEmpty;

      if (translations.Count != otherTranslations.Count)
         return false;

      foreach (var kvp in translations)
      {
         if (!otherTranslations.TryGetValue(kvp.Key, out var otherValue))
            return false;

         if (kvp.Value != otherValue)
            return false;
      }

      return true;
   }

   public override int GetHashCode()
   {
      unchecked
      {
         var hashCode = Namespace?.GetHashCode() ?? 0;
         hashCode = (hashCode * 397) ^ Name.GetHashCode();
         hashCode = (hashCode * 397) ^ Translations.Count.GetHashCode();

         return hashCode;
      }
   }
}
