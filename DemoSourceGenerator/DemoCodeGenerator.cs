using System.Text;

namespace DemoSourceGenerator;

public sealed class DemoCodeGenerator : ICodeGenerator
{
   public static readonly DemoCodeGenerator Instance = new();

   private static int _counter;

   public string FileHintSuffix => ".Main";

   public string Generate(DemoEnumInfo enumInfo, GeneratorOptions options)
   {
      var ns = enumInfo.Namespace;
      var name = enumInfo.Name;

      var sb = new StringBuilder(@"// <auto-generated />
#nullable enable");

      if (options.CounterEnabled)
      {
         sb.Append($@"

// generation counter: {Interlocked.Increment(ref _counter)}");
      }

      sb.Append($@"

using System.Collections.Generic;
using DemoLibrary;

{(ns is null ? null : $@"namespace {ns}
{{")}
   partial class {name}
   {{
      private static IReadOnlyList<{name}>? _items;
      public static IReadOnlyList<{name}> Items => _items ??= GetItems();

      private static IReadOnlyList<{name}> GetItems()
      {{
         return new[] {{ {String.Join(", ", enumInfo.ItemNames)} }};
      }}
   }}
{(ns is null ? null : @"}
")}");

      return sb.ToString();
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
