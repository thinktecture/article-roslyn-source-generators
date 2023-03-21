namespace DemoSourceGenerator;

public interface ICodeGenerator : IEquatable<ICodeGenerator>
{
   string? FileHintSuffix { get; }

   string Generate(DemoEnumInfo enumInfo, GeneratorOptions options);
}
