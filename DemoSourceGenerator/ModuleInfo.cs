namespace DemoSourceGenerator;

public readonly struct ModuleInfo
{
   public string Name { get; }
   public Version Version { get; }

   public ModuleInfo(string name, Version version)
   {
      Name = name;
      Version = version;
   }
}
