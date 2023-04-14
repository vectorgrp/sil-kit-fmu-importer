namespace Fmi
{
  public class Helpers
  {
    public enum LogSeverity { Error, Warning, Information, Debug, Trace }

    private static Action<LogSeverity, string>? _loggerAction;

    public static void SetLoggerCallback(Action<LogSeverity, string> callback)
    {
      _loggerAction = callback;
    }

    public static void Log(LogSeverity severity, string message)
    {
      _loggerAction?.Invoke(severity, message);
    }
  }
}
