namespace DemoSourceGenerator.Logging;

public class NullLogger : ILogger
{
   public static readonly ILogger Instance = new NullLogger();

   public bool IsEnabled(LogLevel logLevel)
   {
      return false;
   }

   public void Log(LogLevel logLevel, string message)
   {
   }
}
