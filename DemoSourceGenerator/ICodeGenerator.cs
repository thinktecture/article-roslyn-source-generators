namespace DemoSourceGenerator;

public interface ICodeGenerator : IEquatable<ICodeGenerator>
{
   string Generate(DemoEnumInfo enumInfo);
}
