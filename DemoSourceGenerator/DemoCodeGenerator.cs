namespace DemoSourceGenerator;

public sealed class DemoCodeGenerator : ICodeGenerator
{
   public static readonly DemoCodeGenerator Instance = new();

   private static int _counter;

   public string Generate(DemoEnumInfo enumInfo)
   {
      var ns = enumInfo.Namespace;
      var name = enumInfo.Name;

      return @$"// <auto-generated />

// generation counter: {Interlocked.Increment(ref _counter)}

using System.Collections.Generic;

{(ns is null ? null : $@"namespace {ns}
{{")}
   partial class {name}
   {{
      private static IReadOnlyList<{name}> _items;
      public static IReadOnlyList<{name}> Items => _items ??= GetItems();

      private static IReadOnlyList<{name}> GetItems()
      {{
         return new[] {{ {String.Join(", ", enumInfo.ItemNames)} }};
      }}
   }}
{(ns is null ? null : @"}
")}";
   }

   public override bool Equals(object? obj)
   {
      return obj is DemoCodeGenerator;
   }

   public bool Equals(ICodeGenerator other)
   {
      return other is DemoCodeGenerator;
   }

   public override int GetHashCode()
   {
      return GetType().GetHashCode();
   }
}
