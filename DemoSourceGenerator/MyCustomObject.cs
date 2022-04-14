namespace DemoSourceGenerator;

public readonly struct MyCustomObject : IEquatable<MyCustomObject>
{
   public string Name { get; }

   public MyCustomObject(string name)
   {
      Name = name;
   }

   public override bool Equals(object? obj)
   {
      return obj is MyCustomObject customObject
             && Equals(customObject);
   }

   public bool Equals(MyCustomObject other)
   {
      return Name == other.Name;
   }

   public override int GetHashCode()
   {
      return Name.GetHashCode();
   }
}
