using System.Diagnostics;

namespace DemoSourceGenerator.Logging;

public class SelfLog
{
   private const string _FILE_NAME = "DemoSourceGenerator.log";

   public static void Write(string message)
   {
      try
      {
         var fullPath = Path.Combine(Path.GetTempPath(), _FILE_NAME);
         File.AppendAllText(fullPath, $"[{DateTime.Now:O}] {message}{Environment.NewLine}");
      }
      catch (Exception ex)
      {
         Debug.WriteLine(ex);
      }
   }
}
