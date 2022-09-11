namespace DemoSourceGenerator;

public interface ICodeGenerator : IEquatable<ICodeGenerator>
{
   string? FileHintSuffix { get; }

   string Generate(DemoEnumInfo enumInfo, IReadOnlyDictionary<string, string> translations);
}
