using System.Text;

namespace DemoSourceGenerator;

public sealed class NewtonsoftJsonSourceGenerator : ICodeGenerator
{
   public static readonly NewtonsoftJsonSourceGenerator Instance = new();

   private static int _counter;

   public string FileHintSuffix => ".NewtonsoftJson";

   public string Generate(DemoEnumInfo enumInfo, GeneratorOptions options)
   {
      if (!enumInfo.HasNameProperty)
         return String.Empty;

      var ns = enumInfo.Namespace;
      var name = enumInfo.Name;

      var sb = new StringBuilder(@"// <auto-generated />
#nullable enable");

      if (options.CounterEnabled)
      {
         sb.Append($@"

// generation counter: {Interlocked.Increment(ref _counter)}");
      }

      sb.Append(@$"

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

{(ns is null ? null : $@"namespace {ns}
{{")}
   [JsonConverterAttribute(typeof({name}NewtonsoftJsonConverter))]
   partial class {name}
   {{
      public class {name}NewtonsoftJsonConverter : JsonConverter<{name}>
      {{
         public override void WriteJson(JsonWriter writer, {name}? value, JsonSerializer serializer)
         {{
            if (value is null)
            {{
               writer.WriteNull();
            }}
            else
            {{
               writer.WriteValue(value.Name);
            }}
         }}

         public override {name}? ReadJson(JsonReader reader, Type objectType, {name}? existingValue, bool hasExistingValue, JsonSerializer serializer)
         {{
            var name = serializer.Deserialize<string?>(reader);

            return name is null
                      ? null
                      : {name}.Items.SingleOrDefault(c => c.Name == name);
         }}
      }}
   }}
{(ns is null ? null : @"}
")}");

      return sb.ToString();
   }

   public override bool Equals(object? obj)
   {
      return obj is NewtonsoftJsonSourceGenerator;
   }

   public bool Equals(ICodeGenerator other)
   {
      return other is NewtonsoftJsonSourceGenerator;
   }

   public override int GetHashCode()
   {
      return GetType().GetHashCode();
   }
}
